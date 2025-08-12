# NetVizor 单文件发布脚本
Write-Host "开始发布 NetVizor 单文件版本..." -ForegroundColor Green

# 清理之前的发布文件
$publishPath = "publish\exe"
if (Test-Path $publishPath) {
    Remove-Item $publishPath -Recurse -Force
    Write-Host "已清理旧的发布文件" -ForegroundColor Yellow
}
New-Item -ItemType Directory -Path $publishPath -Force | Out-Null

# 发布为单文件可执行程序
Write-Host "正在编译和打包..." -ForegroundColor Cyan
$publishResult = dotnet publish Shell\Shell.csproj `
    --configuration Release `
    --output $publishPath `
    --self-contained true `
    --runtime win-x64 `
    --property:PublishSingleFile=true `
    --property:IncludeNativeLibrariesForSelfExtract=true `
    --property:DebugType=None `
    --property:DebugSymbols=false

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "发布成功！" -ForegroundColor Green
    Write-Host "输出目录: $publishPath" -ForegroundColor White
    Write-Host "可执行文件: $publishPath\Shell.exe" -ForegroundColor White
    
    # 显示文件信息
    Write-Host ""
    Write-Host "发布文件列表:" -ForegroundColor Cyan
    Get-ChildItem $publishPath | Format-Table Name, Length, LastWriteTime -AutoSize
    
    # 显示主要文件大小
    $exeFile = "$publishPath\Shell.exe"
    if (Test-Path $exeFile) {
        $size = (Get-Item $exeFile).Length / 1MB
        Write-Host "主程序大小: $([math]::Round($size, 2)) MB" -ForegroundColor Yellow
    }
} else {
    Write-Host "发布失败！请检查错误信息。" -ForegroundColor Red
}

Write-Host ""
Write-Host "按任意键继续..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")