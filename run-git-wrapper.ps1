#!/usr/bin/env pwsh
# Wrapper script to set JJ_NET_REPO_PATH and run Run-Git.ps1

$env:JJ_NET_REPO_PATH = 'C:\Users\mathew.burkitt\source\repos\DT\JJ.Net'

& 'C:\Users\mathew.burkitt\source\repos\DT\ReCode\source\Ops\code\powershell\Dev\Run-Git\Run-Git.ps1' -Auto -Live
