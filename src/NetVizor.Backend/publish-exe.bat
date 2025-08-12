@echo off
echo 开始发布 NetVizor 单文件版本...

REM 清理之前的发布文件
if exist "publish\exe" rmdir /s /q "publish\exe"
mkdir "publish\exe"

REM 发布为单文件可执行程序
dotnet publish Shell\Shell.csproj ^
    --configuration Release ^
    --output "publish\exe" ^
    --self-contained true ^
    --runtime win-x64 ^
    --property:PublishSingleFile=true ^
    --property:IncludeNativeLibrariesForSelfExtract=true ^
    --property:DebugType=None ^
    --property:DebugSymbols=false

if %ERRORLEVEL% equ 0 (
    echo.
    echo 发布成功！
    echo 输出目录: publish\exe
    echo 可执行文件: publish\exe\Shell.exe
    echo.
    echo 文件列表:
    dir "publish\exe" /b
    echo.
    pause
) else (
    echo 发布失败！
    pause
)