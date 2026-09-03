using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace GUIDiskpart
{
    public partial class MainWindow : Window
    {
        private DiskInfo? _selectedDisk;
        private PartitionInfo? _selectedPartition;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDisksAndPartitionsAsync();
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDisksAndPartitionsAsync();
        }

        private void SetBusyState(bool isBusy, string statusMessage)
        {
            btnRefresh.IsEnabled = !isBusy;
            OperationPanel.IsEnabled = !isBusy;
            progBusy.Visibility = isBusy ? Visibility.Visible : Visibility.Hidden;
            txtStatus.Text = statusMessage;
            txtStatus.Foreground = isBusy ? System.Windows.Media.Brushes.DarkOrange : System.Windows.Media.Brushes.Blue;
        }

        private async Task<string> RunStorageCommandAsync(string command)
        {
            return await Task.Run(() =>
            {
                using Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; {command}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0) throw new Exception(error);
                return output;
            });
        }

        private async Task LoadDisksAndPartitionsAsync()
        {
            try
            {
                SetBusyState(true, "ディスク・パーティション情報をスキャン中...");

                string psCommand = @"
                    $disks = @(Get-Disk | Select-Object Number, FriendlyName, Size, IsSystem, IsBoot, BusType);
                    foreach ($d in $disks) {
                        $parts = @(Get-Partition -DiskNumber $d.Number -ErrorAction SilentlyContinue | Select-Object DiskNumber, PartitionNumber, @{N='DriveLetter';E={if($_.DriveLetter -ne 0){[string]$_.DriveLetter}else{''}}}, Size, Type);
                        $d | Add-Member -MemberType NoteProperty -Name Partitions -Value $parts -PassThru | Out-Null
                    }
                    ConvertTo-Json -InputObject $disks -Depth 3
                ";

                string json = await RunStorageCommandAsync(psCommand);
                if (string.IsNullOrWhiteSpace(json)) return;

                var disks = JsonSerializer.Deserialize<List<DiskInfo>>(json);
                treeDisks.ItemsSource = disks;

                _selectedDisk = null;
                _selectedPartition = null;
                SetBusyState(false, "読み込み完了");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"読み込みエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                SetBusyState(false, "読み込みエラーが発生しました。");
            }
        }

        private void treeDisks_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _selectedDisk = null;
            _selectedPartition = null;

            // 一旦すべてのボタンを無効化
            btnClean.IsEnabled = false;
            btnPartition.IsEnabled = false;
            btnEject.IsEnabled = false;
            btnFormat.IsEnabled = false;
            btnAssign.IsEnabled = false;
            btnDeletePartition.IsEnabled = false;

            var selected = treeDisks.SelectedItem;

            if (selected is DiskInfo disk)
            {
                _selectedDisk = disk;
                txtSelectedTarget.Text = $"選択項目: Disk {disk.Number} ({disk.FriendlyName})";

                if (disk.IsDangerous || disk.IsInternal)
                {
                    txtStatus.Text = "システムまたは内蔵ディスクのため、安全を考慮し操作をロックしています。";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    // 外部ディスク（USB等）の場合のみ、イジェクトを含めたディスク操作を有効化
                    btnClean.IsEnabled = true;
                    btnPartition.IsEnabled = true;
                    btnEject.IsEnabled = true;
                    txtStatus.Text = "外部ディスク(USB等)が選択されました。操作可能です。";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Blue;
                }
            }
            else if (selected is PartitionInfo part)
            {
                _selectedPartition = part;
            }

            if (_selectedPartition != null)
            {
                txtSelectedTarget.Text = $"選択項目: Disk {_selectedPartition.DiskNumber} / Partition {_selectedPartition.PartitionNumber}";

                var disks = treeDisks.ItemsSource as List<DiskInfo>;
                var parentDisk = disks?.FirstOrDefault(d => d.Number == _selectedPartition.DiskNumber);

                if (parentDisk != null && (parentDisk.IsDangerous || parentDisk.IsInternal))
                {
                    txtStatus.Text = "保護対象（システムまたは内蔵）のパーティションのため操作できません。";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (_selectedPartition.Type == "Reserved" || _selectedPartition.Type == "System")
                {
                    txtStatus.Text = "システム/予約パーティションのため操作できません。";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    btnFormat.IsEnabled = true;
                    btnAssign.IsEnabled = true;
                    btnDeletePartition.IsEnabled = true;
                    txtStatus.Text = "パーティション操作が可能です。";
                    txtStatus.Foreground = System.Windows.Media.Brushes.Blue;
                }
            }
        }

        private async void btnClean_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDisk == null) return;
            if (MessageBox.Show($"Disk {_selectedDisk.Number} を全消去(初期化)します。\nすべてのデータが失われます。よろしいですか？",
                "最終警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            string cmd = $"Set-Disk -Number {_selectedDisk.Number} -IsOffline $false -ErrorAction SilentlyContinue; " +
                         $"Set-Disk -Number {_selectedDisk.Number} -IsReadOnly $false -ErrorAction SilentlyContinue; " +
                         $"Clear-Disk -Number {_selectedDisk.Number} -RemoveData -RemoveOEM -Confirm:$false";

            await ExecuteOperationAsync(cmd, "Cleanを処理中...");
        }

        private async void btnPartition_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDisk == null) return;

            string cmd = $"Initialize-Disk -Number {_selectedDisk.Number} -PartitionStyle MBR -ErrorAction SilentlyContinue; " +
                         $"Start-Sleep -Seconds 2; " +
                         $"New-Partition -DiskNumber {_selectedDisk.Number} -UseMaximumSize";

            await ExecuteOperationAsync(cmd, "パーティションを作成中...");
        }

        // --- ⏏️ 新規追加：安全な取り外し（イジェクト）処理 ---
        private async void btnEject_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDisk == null) return;

            // 言語環境（日本語/英語）に依存せず、Shell COM経由で確実にイジェクトする堅牢なスクリプト
            string cmd = $@"
                $diskNum = {_selectedDisk.Number};
                $parts = Get-Partition -DiskNumber $diskNum -ErrorAction SilentlyContinue;
                $ejected = $false;
                $sh = New-Object -ComObject Shell.Application;
                $ns = $sh.Namespace(17); # 17 = ssfDRIVES (PC / マイ コンピューター)

                foreach ($p in $parts) {{
                    if ($p.DriveLetter -and $p.DriveLetter -ne 0) {{
                        $letter = ([string]$p.DriveLetter).Trim() + ':';
                        $item = $ns.ParseName($letter);
                        if ($item) {{
                            # 日本語('取り出し')または英語('Eject')の動詞を探して実行
                            $verb = $item.Verbs() | Where-Object {{ $_.Name -match '取り出し|Eject|切断|E&ject|安全' }} | Select-Object -First 1;
                            if ($verb) {{
                                $verb.DoIt();
                                $ejected = $true;
                            }} else {{
                                $item.InvokeVerb('Eject');
                                $ejected = $true;
                            }}
                        }}
                    }}
                }}

                # ドライブレターがない未フォーマット等の場合は、安全にキャッシュを吐き出してオフライン化する
                if (-not $ejected) {{
                    Set-Disk -Number $diskNum -IsOffline $true -ErrorAction SilentlyContinue;
                }}
            ";

            SetBusyState(true, "安全に取り外し処理を実行中...");
            try
            {
                await RunStorageCommandAsync(cmd);
                MessageBox.Show($"Disk {_selectedDisk.Number} ({_selectedDisk.FriendlyName}) を安全に取り外せる状態にしました。\nデバイスを抜いて構いません。",
                                "安全な取り外し完了", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDisksAndPartitionsAsync(); // リストから消滅したことを反映
            }
            catch (Exception ex)
            {
                MessageBox.Show($"取り外し失敗: {ex.Message}\nファイルを開いているプログラムがないか確認してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                SetBusyState(false, "エラーが発生しました。");
            }
        }

        private async void btnFormat_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPartition == null) return;

            string cmd = $@"
                $part = Get-Partition -DiskNumber {_selectedPartition.DiskNumber} -PartitionNumber {_selectedPartition.PartitionNumber} -ErrorAction Stop;
                if (-not $part) {{ throw '対象のパーティションがシステム上に見つかりません。' }}
                Start-Sleep -Seconds 2;
                Format-Volume -Partition $part -FileSystem NTFS -Confirm:$false;
            ";

            await ExecuteOperationAsync(cmd, "NTFSでフォーマットを実行中...");
        }

        private async void btnAssign_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPartition == null) return;
            string cmd = $"Get-Partition -DiskNumber {_selectedPartition.DiskNumber} -PartitionNumber {_selectedPartition.PartitionNumber} | Add-PartitionAccessPath -AssignDriveLetter";
            await ExecuteOperationAsync(cmd, "ドライブレターを自動割当中...");
        }

        private async void btnDeletePartition_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPartition == null) return;
            if (MessageBox.Show($"Disk {_selectedPartition.DiskNumber} の Partition {_selectedPartition.PartitionNumber} を削除します。\nよろしいですか？",
                "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            string cmd = $"$err = $false; try {{ Remove-Partition -DiskNumber {_selectedPartition.DiskNumber} -PartitionNumber {_selectedPartition.PartitionNumber} -Confirm:$false -ErrorAction Stop }} catch {{ $err = $true }}; " +
                         $"if ($err) {{ Set-Disk -Number {_selectedPartition.DiskNumber} -IsReadOnly $false; Clear-Disk -Number {_selectedPartition.DiskNumber} -RemoveData -RemoveOEM -Confirm:$false }}";

            await ExecuteOperationAsync(cmd, "パーティションを削除中...");
        }

        private async Task ExecuteOperationAsync(string command, string busyMessage)
        {
            SetBusyState(true, busyMessage);
            try
            {
                await RunStorageCommandAsync(command);
                MessageBox.Show("処理が正常に完了しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadDisksAndPartitionsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失敗: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                SetBusyState(false, "エラーが発生しました。");
            }
        }
    }

    public class DiskInfo
    {
        [JsonPropertyName("Number")]
        public int Number { get; set; }
        [JsonPropertyName("FriendlyName")]
        public string? FriendlyName { get; set; }
        [JsonPropertyName("Size")]
        public long Size { get; set; }
        [JsonPropertyName("IsSystem")]
        public bool IsSystem { get; set; }
        [JsonPropertyName("IsBoot")]
        public bool IsBoot { get; set; }
        [JsonPropertyName("BusType")]
        public string? BusType { get; set; }
        [JsonPropertyName("Partitions")]
        public List<PartitionInfo>? Partitions { get; set; }

        public string SizeGB => $"{(Size / 1024.0 / 1024.0 / 1024.0):F2}";
        public bool IsDangerous => IsSystem || IsBoot;
        public bool IsInternal => BusType != "USB" && !IsDangerous;
        public string WarningLabel => IsDangerous ? " [OS保護]" : (IsInternal ? " [内蔵注意]" : "");
    }

    public class PartitionInfo
    {
        [JsonPropertyName("DiskNumber")]
        public int DiskNumber { get; set; }
        [JsonPropertyName("PartitionNumber")]
        public int PartitionNumber { get; set; }
        [JsonPropertyName("DriveLetter")]
        public string? DriveLetter { get; set; }
        [JsonPropertyName("Size")]
        public long Size { get; set; }
        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        public string SizeGB => $"{(Size / 1024.0 / 1024.0 / 1024.0):F2}";
        public string DriveLetterString => !string.IsNullOrEmpty(DriveLetter) ? $"[{DriveLetter}:]" : "";
    }
}