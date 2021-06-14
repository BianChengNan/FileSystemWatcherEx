$curPath = $PSScriptRoot
Set-Location $curPath

$allImages = Get-ChildItem -Path $curPath -Filter "*.bmp"

for ($idx = 0; $idx -le 1000; ++$idx)
{
    foreach ($item in $allImages)
    {
        Copy-Item $item.FullName (Join-Path $curPath "..\\monitorFolder\\")
        Start-Sleep(1)
    }
}
