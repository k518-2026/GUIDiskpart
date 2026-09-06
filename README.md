# 💾 GUI DiskPart Studio (`GUIDiskpart`)

> A C# / WPF utility application that allows you to operate Windows' complex disk management commands (DiskPart / Storage API) via a safe, intuitive, and modern GUI[cite: 2].

---

## 🌟 Features

*   **Fully GUI-based:** Eliminates command-line memorization and typos, enabling disk management through intuitive tree-view operations[cite: 2].
*   **Ironclad Failsafe (Safety First):** Automatically detects the system drive where the OS is installed and internal disks, **completely locking operation buttons** to prevent destruction from accidental operations[cite: 2].
*   **USB Hot-Plug Support:** Even if a USB flash drive is inserted or removed after launching the app, the "Rescan Disk Information" button instantly scans the latest state[cite: 2].
*   **Asynchronous Processing (Async/Await):** The UI does not freeze even during heavy I/O processing, and a progress bar notifies you of visual progress and wait states[cite: 2].
*   **Complete Prevention of Garbled Text:** Completely unifies JSON data exchange with PowerShell to UTF-8, ensuring stable operation in both Japanese and English environments[cite: 2].

---

## 📸 Screenshots / UI Layout

```text
+-------------------------------------------------------------+
| 🔄 Rescan Disk Information (USB Support)                      |
+-----------------------------+-------------------------------+
| 💾 Disk 1 [USB] 14.90GB     | [ Selected Item: Disk 1 (USB Drive) ]|
|   └ Partition 1 [E:]        |                               |
| 💾 Disk 2 [SATA] 476.94GB   | [ Operations for Entire Disk ]  |
|   [OS Protection]           |  - 1. Clean (Erase All / Initialize)|
|                             |  - 2. Create Max Partition      |
|                             |  - ⏏️ Safely Remove (Eject)     |
|                             |                               |
|                             | [ Operations for Selected Partition ]|
|                             |  - 3. Format (NTFS)             |
|                             |  - 4. Auto-Assign Drive Letter  |
|                             |  - 5. Delete Partition          |
+-----------------------------+-------------------------------+



# 💾 GUI DiskPart Studio (`GUIDiskpart`)

> Windowsの複雑なディスク管理コマンド（DiskPart / Storage API）を、安全・直感的・モダンなGUIで操作できるC# / WPFユーティリティアプリケーション。

---

## 🌟 特徴 (Features)

*   **完全なGUI化:** コマンドラインの暗記や誤入力を排除し、直感的なツリービュー操作でディスク管理を実現。
*   **鉄壁のフェイルセーフ (Safety First):** OSがインストールされたシステムドライブや内蔵ディスクを自動検知し、誤操作による破壊を防ぐため**操作ボタンを完全にロック**。
*   **USBホットプラグ対応:** アプリ起動後にUSBメモリを抜き差ししても、「ディスク情報を再認識」ボタンで即座に最新状態をスキャン。
*   **非同期処理 (Async/Await):** 重いI/O処理中もUIがフリーズせず、プログレスバーによる視覚的な進捗・待機状態をお知らせ。
*   **文字化け完全防止:** PowerShellとのJSONデータやり取りをUTF-8に完全統一し、日本語環境・英語環境の両方で安定動作。

---

---

## 🛠️ 主な機能一覧

| 区分 | 機能名 | 説明 |
| --- | --- | --- |
| **スキャン** | ディスク情報の再認識 | 接続されている全ての物理ディスクとパーティション階層を一括取得。 |
| **全体操作** | 1. Clean (初期化) | 読み取り専用やOEM領域を強制解除し、ディスクを完全消去。 |
| **全体操作** | 2. 最大パーティション作成 | MBR形式で初期化し、未割り当て領域全体を使うパーティションを構築。 |
| **全体操作** | ⏏️ 安全に取り外す | Windows Shellと連携し、USBメモリ等のキャッシュを吐き出して安全にイジェクト。 |
| **部分操作** | 3. フォーマット (NTFS) | 指定したパーティションをNTFS形式で確実に初期化。 |
| **部分操作** | 4. ドライブレター自動割当 | エクスプローラーからアクセスできるよう、空き文字を自動割当。 |
| **部分操作** | 5. パーティション削除 | 指定した領域のみをピンポイントで未割り当てに戻す。 |

---

## 🔒 安全設計（カラーインジケーター）

ツリービューの背景色により、現在のディスクの危険度が視覚的に一目でわかります。

* **赤色（OS保護）**：Windowsシステムドライブ。**すべての操作が厳格に禁止されます。**
* **黄色（内蔵注意）**：SATA / NVMeなどの内蔵ディスク。重要データ保護のため**操作がロックされます。**
* **通常カラー（外部ディスク）**：USBメモリや外付けHDD。**安全にすべての操作が可能です。**

---

## 🚀 動作環境 (Requirements)

* **OS:** Windows 10 / 11 (64bit)
* **ランタイム:** .NET 8.0 デスクトップランタイム (※自己完結型発行時は不要)
* **権限:** **管理者権限必須** (低レイヤーのStorage APIを操作するため、自動でUAC昇格を要求します)

---

## 💻 開発・ビルド手順 (Getting Started)

1. リポジトリをクローンまたはダウンロードします。
2. Visual Studio (2022推奨) で `GUIDiskpart.sln` を開きます。
3. ソリューションの構成を `Release`、ターゲットを `win-x64` に設定します。
4. **「ビルド」 ＞ 「ソリューションのリビルド」** を実行します。
5. 必要に応じてプロジェクトのプロパティからカスタムアイコン（`.ico`）を埋め込んでください。

---

## 📄 ライセンス (License)

このプロジェクトはMITライセンスです。個人利用および学習用として自由に改変・利用して構いません。

```

```
