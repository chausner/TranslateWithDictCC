param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "TranslateWithDictCC\Assets\Subjects.json")
)

$ErrorActionPreference = "Stop"

function Get-LanguagePairs
{
    $response = Invoke-WebRequest -Uri "https://browse.dict.cc/"
    $html = $response.Content

    [regex]::Matches($html, '(?i)<option\b[^>]*value="([A-Z]{4})"')
        | ForEach-Object { $_.Groups[1].Value.ToUpperInvariant() }
        | Select-Object -Unique
        | Sort-Object
}

function Get-PlainText([string]$HtmlFragment)
{
    # remove HTML tags like <a> and <b>
    $plainText = [regex]::Replace($HtmlFragment, '<[^>]+>', '')

    # unescape HTML-encoded characters (e.g. &nbsp;)
    $plainText = [System.Net.WebUtility]::HtmlDecode($plainText).Trim()

    # remove extraneous whitespace
    $plainText = [regex]::Replace($plainText, '\s{2,}', ' ')

    $plainText
}

function Get-SubjectsForPair([string]$LanguagePair)
{
    $response = Invoke-WebRequest -Uri "https://$($LanguagePair.ToLowerInvariant()).dict.cc/subjects.php"
    $html = $response.Content

    $tables = [regex]::Matches($html, '<table\b[^>]*>(.*?)</table>', 'IgnoreCase,Singleline')
    $subjectsTableHtml = $tables[2].Value

    $rows = [regex]::Matches($subjectsTableHtml, '<tr\b[^>]*>(.*?)</tr>', 'IgnoreCase,Singleline')
    $subjectsRows = $rows | Select-Object -Skip 1 # skip header row
    
    $subjectsForPair = [ordered]@{}

    foreach ($row in $subjectsRows)
    {
        $rowHtml = $row.Groups[1].Value
        $cells = [regex]::Matches($rowHtml, '<td\b[^>]*>(.*?)</td>', 'IgnoreCase,Singleline')
        $abbreviation = Get-PlainText $cells[0].Groups[1].Value
        $description = Get-PlainText $cells[1].Groups[1].Value
        $subjectsForPair[$abbreviation] = $description
    }

    $subjectsForPair
}

Write-Host "Scraping list of language pairs... " -NoNewline
$languagePairs = Get-LanguagePairs
Write-Host "$($languagePairs.Count) pairs."

$subjects = [ordered]@{}

foreach ($languagePair in $languagePairs)
{
    Write-Host "Scraping language pair $languagePair... " -NoNewline
    $subjectsForPair = Get-SubjectsForPair -LanguagePair $languagePair
    $subjects[$languagePair] = $subjectsForPair
    Write-Host "$($subjectsForPair.Count) subjects."
}

$json = $subjects | ConvertTo-Json
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($OutputPath, $json, $utf8NoBom)

Write-Host "Subjects saved to $OutputPath."
