#Requires -Version 7.0

param(
    [string]$Path = "src",
    [ValidateSet("Table", "List", "CSV", "JSON")]
    [string]$OutputFormat = "Table",
    [string]$Filter = "*",
    [ValidateSet("Pattern", "File", "Severity")]
    [string]$GroupBy = "Pattern",
    [switch]$ShowExempted
)

class Finding {
    [string]$File
    [int]$Line
    [string]$Pattern
    [string]$Severity
    [string]$Code
    [string]$Suggestion
    [bool]$Exempted
    [string]$ExemptionReason
}

$patterns = @{
    "Nullable-Task-Return" = @{
        Regex = '(public|private|protected|internal)\s+(async\s+)?Task<\w+\?>'
        Severity = "High"
        Suggestion = "Change return type to Task<Result<T>> to properly report errors"
        CheckId = "SF001"
    }
    "Try-Catch-Return-Null" = @{
        Regex = 'catch\s*(\([^)]*\))?\s*\{[^}]*return\s+null'
        Severity = "High"
        Suggestion = "Return Result<T>.Fail() with error details instead of null"
        CheckId = "SF002"
    }
    "Try-Catch-Throw" = @{
        Regex = 'catch\s*(\([^)]*\))?\s*\{[^}]*throw'
        Severity = "Medium"
        Suggestion = "Consider returning Result<T>.Fail() instead of throwing per CLAUDE.md"
        CheckId = "SF003"
    }
    "Async-Void" = @{
        Regex = 'async\s+void\s+\w+'
        Severity = "High"
        Suggestion = "Change to async Task or async Task<Result> to enable error propagation"
        CheckId = "SF004"
    }
}

$csFiles = Get-ChildItem -Path $Path -Filter "*.cs" -Recurse | Where-Object { $_.Name -like $Filter }

Write-Host "Scanning $($csFiles.Count) C# files in '$Path'..." -ForegroundColor Cyan
Write-Host ""

$findings = @()
$fileCount = 0
$totalLines = 0

foreach ($file in $csFiles) {
    $fileCount++
    $content = Get-Content $file.FullName -Raw
    $lines = Get-Content $file.FullName

    $totalLines += $lines.Count

    $hasExemptions = $content -match '\[SuppressMessage\("SilentFailure"'

    foreach ($patternName in $patterns.Keys) {
        $pattern = $patterns[$patternName]
        $regex = [regex]::new($pattern.Regex, [System.Text.RegularExpressions.RegexOptions]::Multiline)

        $matches = $regex.Matches($content)

        foreach ($match in $matches) {
            $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count

            $codeLine = if ($lineNumber -le $lines.Count) {
                $lines[$lineNumber - 1].Trim()
            } else {
                $match.Value.Trim()
            }

            $isExempted = $false
            $exemptionReason = ""

            if ($hasExemptions) {
                $beforeMatch = $content.Substring(0, $match.Index)
                $lastSuppressMessage = [regex]::Matches($beforeMatch, '\[SuppressMessage\("SilentFailure",\s*"([^"]+)"[^]]*Justification\s*=\s*"([^"]+)"\)\]')

                if ($lastSuppressMessage.Count -gt 0) {
                    $lastAttr = $lastSuppressMessage[$lastSuppressMessage.Count - 1]
                    $checkId = $lastAttr.Groups[1].Value
                    $justification = $lastAttr.Groups[2].Value

                    $attrLineNumber = ($beforeMatch.Substring(0, $lastAttr.Index) -split "`n").Count
                    if ($lineNumber - $attrLineNumber -le 10 -and $checkId -eq $pattern.CheckId) {
                        $isExempted = $true
                        $exemptionReason = $justification
                    }
                }
            }

            if ($isExempted -and -not $ShowExempted) {
                continue
            }

            $finding = [Finding]@{
                File = $file.FullName.Replace((Get-Location).Path, ".").Replace("\", "/")
                Line = $lineNumber
                Pattern = $patternName
                Severity = $pattern.Severity
                Code = $codeLine
                Suggestion = $pattern.Suggestion
                Exempted = $isExempted
                ExemptionReason = $exemptionReason
            }

            $findings += $finding
        }
    }
}

Write-Host "Scanned $fileCount files ($totalLines lines)" -ForegroundColor Cyan
Write-Host ""

$totalFindings = $findings.Count
$exemptedCount = ($findings | Where-Object { $_.Exempted }).Count
$activeCount = $totalFindings - $exemptedCount

Write-Host "=======================================================" -ForegroundColor Yellow
Write-Host "  Silent Failure Detection Report" -ForegroundColor Yellow
Write-Host "=======================================================" -ForegroundColor Yellow
Write-Host ""
Write-Host "Total Findings:    $totalFindings" -ForegroundColor $(if ($totalFindings -gt 0) { "Red" } else { "Green" })
Write-Host "Active Findings:   $activeCount" -ForegroundColor $(if ($activeCount -gt 0) { "Red" } else { "Green" })
Write-Host "Exempted:          $exemptedCount" -ForegroundColor Gray
Write-Host ""

if ($totalFindings -eq 0) {
    Write-Host "No silent failures detected!" -ForegroundColor Green
    Write-Host ""
    exit 0
}

$grouped = switch ($GroupBy) {
    "Pattern" { $findings | Group-Object -Property Pattern }
    "File" { $findings | Group-Object -Property File }
    "Severity" { $findings | Group-Object -Property Severity }
}

switch ($OutputFormat) {
    "Table" {
        foreach ($group in $grouped) {
            $groupName = $group.Name
            $groupCount = $group.Count

            Write-Host "-------------------------------------------------------" -ForegroundColor Gray
            Write-Host "Pattern: $groupName - $groupCount findings" -ForegroundColor Cyan
            Write-Host "-------------------------------------------------------" -ForegroundColor Gray

            $group.Group | Format-Table -Property File, Line, Severity, @{Label="Code (truncated)"; Expression={$_.Code.Substring(0, [Math]::Min(50, $_.Code.Length))}} -AutoSize

            $firstSuggestion = $group.Group[0].Suggestion
            Write-Host "Suggestion: $firstSuggestion" -ForegroundColor Yellow
            Write-Host ""
        }
    }

    "List" {
        foreach ($group in $grouped) {
            $groupName = $group.Name
            Write-Host ""
            Write-Host "=== $GroupBy : $groupName ===" -ForegroundColor Cyan

            foreach ($finding in $group.Group) {
                Write-Host ""
                Write-Host "File:       $($finding.File):$($finding.Line)" -ForegroundColor White
                Write-Host "Pattern:    $($finding.Pattern)" -ForegroundColor Yellow
                Write-Host "Severity:   $($finding.Severity)" -ForegroundColor $(if ($finding.Severity -eq "High") { "Red" } else { "Yellow" })
                Write-Host "Code:       $($finding.Code)" -ForegroundColor Gray
                Write-Host "Suggestion: $($finding.Suggestion)" -ForegroundColor Yellow

                if ($finding.Exempted) {
                    Write-Host "Exempted:   $($finding.ExemptionReason)" -ForegroundColor Green
                }
            }
        }
    }

    "CSV" {
        $findings | Select-Object File, Line, Pattern, Severity, Code, Suggestion, Exempted, ExemptionReason | ConvertTo-Csv -NoTypeInformation
    }

    "JSON" {
        $findings | Select-Object File, Line, Pattern, Severity, Code, Suggestion, Exempted, ExemptionReason | ConvertTo-Json
    }
}

Write-Host ""
Write-Host "=======================================================" -ForegroundColor Yellow
Write-Host ""

exit $(if ($activeCount -gt 0) { 1 } else { 0 })
