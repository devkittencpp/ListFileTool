# ListFileTool

ListFileTool (formerly ListFileGenerator) is an automation tool for asset gathering that has been migrated from WPF to Avalonia. It now includes additional automation functions to streamline asset management.

## Key Features

- **Automated Asset Gathering:** Simplifies the process of collecting assets.
- **Auto Clean:** Automatically deletes files as specified in your listfile.
- **Dynamic Configuration:** The save button for configuration appears after processing.

## Configuration

Ensure the following paths are set correctly:

- **Output Listfile:** `./output_listfile.txt` *(Leave this as is)*
- **Input Folder:** `path/to/adts`
- **Data Folder:** `path/to/dataAssets`
- **Output Folder:** `path/to/clientPatch`

## System Requirements

- **Operating System:** Linux (compatible with other OS as applicable)
- **Target Framework:** .NET 9.0

## Installation & Build

Clone the repository and build the project using the following commands:

```bash
git clone https://github.com/devkittencpp/ListFileTool.git
cd ListFileTool
dotnet build
```

## Windows
```bash
dotnet publish -r win-x64 -c Release --self-contained -p:PublishSingleFile=true
```
