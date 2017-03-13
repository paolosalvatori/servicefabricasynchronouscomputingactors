
# Get all ETW Sessions with a name like EventFlow-EtwInput-*
Get-EtwTraceSession | Where-Object {$_.Name -like "EventFlow-EtwInput-*"} | Format-Table -Property Name

# Remove all ETW Sessions with a name like EventFlow-EtwInput-*
[string[]]$EtwSessions = Get-EtwTraceSession | Where-Object {$_.Name -like "EventFlow-EtwInput-*"} | Select-Object -ExpandProperty Name
if ($EtwSessions -and $EtwSessions.Length > 0)
{
    Remove-EtwTraceSession -Name $EtwSessions
}
