Add-Type -AssemblyName System.Drawing
$card = New-Object System.Drawing.Bitmap(1200, 630)
$canvas = [System.Drawing.Graphics]::FromImage($card)
$canvas.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$canvas.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$canvas.Clear([System.Drawing.ColorTranslator]::FromHtml('#092a32'))
$accentBrush = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#087f8c'))
$titleBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$subtitleBrush = New-Object System.Drawing.SolidBrush([System.Drawing.ColorTranslator]::FromHtml('#a7ddd7'))
$points = [System.Drawing.Point[]]@([System.Drawing.Point]::new(900,160),[System.Drawing.Point]::new(1100,315),[System.Drawing.Point]::new(900,470))
$canvas.FillPolygon($accentBrush, $points)
$titleFont = [System.Drawing.Font]::new('Arial',120,[System.Drawing.FontStyle]::Bold,[System.Drawing.GraphicsUnit]::Pixel)
$subtitleFont = [System.Drawing.Font]::new('Arial',32,[System.Drawing.FontStyle]::Regular,[System.Drawing.GraphicsUnit]::Pixel)
$canvas.DrawString('AVEC',$titleFont,$titleBrush,90,150)
$canvas.DrawString("Aki's Video Enhancement Components",$subtitleFont,$subtitleBrush,96,325)
$card.Save((Join-Path $PSScriptRoot '../static/img/social-card.png'),[System.Drawing.Imaging.ImageFormat]::Png)
foreach ($resource in @($canvas,$card,$accentBrush,$titleBrush,$subtitleBrush,$titleFont,$subtitleFont)) { $resource.Dispose() }
