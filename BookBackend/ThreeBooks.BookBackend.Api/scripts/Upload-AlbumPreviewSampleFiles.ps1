param(
    [string]$BaseUrl = "http://localhost:5281",
    [string]$Bucket = "bookbackend-dev",
    [string]$ShareCode = "akqfpbdzr2goos"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiProjectRoot = Split-Path -Parent $scriptRoot
$workspaceRoot = Split-Path -Parent $apiProjectRoot
$sampleRoot = Join-Path $workspaceRoot "tmp\album-preview-sample\$ShareCode"

if (-not (Test-Path $sampleRoot)) {
    throw "Sample root not found: $sampleRoot"
}

$files = @(
    @{ Local = Join-Path $sampleRoot "html\page-001-cover.html";    ObjectKey = "albums/$ShareCode/html/page-001-cover.html";    ContentType = "text/html" },
    @{ Local = Join-Path $sampleRoot "html\page-004-content.html";  ObjectKey = "albums/$ShareCode/html/page-004-content.html";  ContentType = "text/html" },
    @{ Local = Join-Path $sampleRoot "assets\cover-hero.svg";       ObjectKey = "albums/$ShareCode/assets/cover-hero.svg";       ContentType = "image/svg+xml" },
    @{ Local = Join-Path $sampleRoot "assets\content-01.svg";       ObjectKey = "albums/$ShareCode/assets/content-01.svg";       ContentType = "image/svg+xml" }
)

foreach ($file in $files) {
    if (-not (Test-Path $file.Local)) {
        throw "Sample file not found: $($file.Local)"
    }

    $uploadUrl = "$BaseUrl/api/files"
    Write-Host "Uploading $($file.ObjectKey)"

    & curl.exe --silent --show-error --fail `
        --request POST $uploadUrl `
        --form "bucket=$Bucket" `
        --form "objectKey=$($file.ObjectKey)" `
        --form "fileName=$([System.IO.Path]::GetFileName($file.Local))" `
        --form "file=@$($file.Local);type=$($file.ContentType)"

    if ($LASTEXITCODE -ne 0) {
        throw "Upload failed for $($file.ObjectKey)"
    }

    Write-Host "Uploaded $($file.ObjectKey)"
}

Write-Host "All sample HTML/assets uploaded."
Write-Host "Next: apply ThreeBooks.BookBackend.Infrastructure/Sql/Albums/02_album_preview_seed.sql"