[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
$ae = [System.Windows.Automation.AutomationElement]
$cond = New-Object System.Windows.Automation.PropertyCondition($ae::ClassNameProperty, "Shell_TrayWnd")
$tray = $ae::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
$bc = New-Object System.Windows.Automation.PropertyCondition($ae::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$all = $tray.FindAll([System.Windows.Automation.TreeScope]::Descendants, $bc)
foreach ($el in $all) {
    $r = $el.Current.BoundingRectangle
    if ($r.Width -gt 1 -and $r.Height -gt 1) {
        $line = "{0,5} {1,5} {2,4} {3,4}  name='{4}'  aid='{5}'" -f [int]$r.X, [int]$r.Y, [int]$r.Width, [int]$r.Height, $el.Current.Name, $el.Current.AutomationId
        Write-Output $line
    }
}
