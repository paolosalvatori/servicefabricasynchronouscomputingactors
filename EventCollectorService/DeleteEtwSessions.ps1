[string[]]$EtwSessions = Get-EtwTraceSession -Name "*"| Where-Object {$_.Name -like "EventFlow-EtwInput-*"} | Select-Object -ExpandProperty Name
if ($EtwSessions -ne $null)
{
    Remove-EtwTraceSession -Name $EtwSessions
}