# get list of language pairs
$response = Invoke-WebRequest -Uri "https://browse.dict.cc/"
$optionElements = @($response.ParsedHtml.body.getElementsByTagName("option"))
$languagePairs = $optionElements.value | where { $_.Length -eq 4 } | select -Unique

# scrape subjects for every language pair
$subjects = @{}
foreach ($languagePair in $languagePairs)
{
    $response = Invoke-WebRequest -Uri "https://$languagePair.dict.cc/subjects.php"
    $table = $response.ParsedHtml.body.getElementsByTagName("table") | where { $_.getAttributeNode('Width').Value -eq '730' }
    $rows = $table.getElementsByTagName("tr") | select -Skip 1

    $pairs = @{}
    foreach ($row in $rows)
    {
        $cells = $row.getElementsByTagName("td")
        $abbreviation = $cells[0].innerText
        $description = $cells[1].innerText
        $pairs[$abbreviation] = $description
    }
    $subjects[$languagePair] = $pairs
}

$subjects | ConvertTo-Json | Out-File (Join-Path $PSScriptRoot "TranslateWithDictCC\Assets\Subjects.json")
