# 用 UI 自动化定位任务栏按钮，输出 "x y w h"，找不到输出所有按钮 + "none"
param([string]$Title, [string]$AppId)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$ae = [System.Windows.Automation.AutomationElement]
$root = $ae::RootElement

$classCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ClassNameProperty, "Shell_TrayWnd")
$tray = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $classCond)
if ($null -eq $tray) { Write-Output "none"; exit 0 }

$btnCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Button)
$all = $tray.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)

if ($AppId) {
    foreach ($el in $all) {
        $id = $el.Current.AutomationId
        if ($id -and $id.Contains($AppId)) {
            $r = $el.Current.BoundingRectangle
            if ($r.Width -gt 1 -and $r.Height -gt 1) {
                Write-Output ([string]::Format("{0} {1} {2} {3}",
                    [int]$r.X, [int]$r.Y, [int]$r.Width, [int]$r.Height))
                exit 0
            }
        }
    }
}

foreach ($el in $all) {
    $name = $el.Current.Name
    if ($Title -and $name -and ($name -eq $Title -or $name.Contains($Title))) {
        $r = $el.Current.BoundingRectangle
        if ($r.Width -gt 1 -and $r.Height -gt 1) {
            Write-Output ([string]::Format("{0} {1} {2} {3}",
                [int]$r.X, [int]$r.Y, [int]$r.Width, [int]$r.Height))
            exit 0
        }
    }
}

foreach ($el in $all) {
    $r = $el.Current.BoundingRectangle
    Write-Output ("btn: [" + $el.Current.Name + "] id=[" + $el.Current.AutomationId + "] " + [int]$r.X + "," + [int]$r.Y + " " + [int]$r.Width + "x" + [int]$r.Height)
}
Write-Output "none"
