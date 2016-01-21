Imports System.Windows.Forms

Public Class VBGame

    ''' <summary>
    ''' Game loop needs to be a thread.
    ''' To use, the display must be set the the form's load event.
    ''' 
    ''' Usage ----------------------------------------------------
    ''' 
    ''' Dim object As New VBGame
    ''' object.setdisplay(form, "[width]x[height]")
    ''' 
    ''' Loop structure -------------------------------------------
    ''' 
    ''' While run
    ''' 
    '''     [do something]    
    ''' 
    '''     vbgame.update() 'Renders the buffer to the screen
    '''     vbgame.clocktick([fps]) 'Waits so that a certain amount of frames pass in a second
    '''     
    ''' End While
    ''' </summary>
    ''' <remarks>Version 0.7</remarks>

    Private WithEvents form As Form
    Public displaybuffer As System.Drawing.BufferedGraphics
    Private displaycontext As System.Drawing.BufferedGraphicsContext
    Public width As Integer
    Public height As Integer

    Public white = Color.FromArgb(255, 255, 255)
    Public black = Color.FromArgb(0, 0, 0)
    Public grey = Color.FromArgb(128, 128, 128)
    Public red = Color.FromArgb(255, 0, 0)
    Public green = Color.FromArgb(0, 255, 0)
    Public blue = Color.FromArgb(0, 0, 255)
    Public cyan = Color.FromArgb(0, 255, 255)
    Public yellow = Color.FromArgb(255, 255, 0)
    Public magenta = Color.FromArgb(255, 0, 255)

    Private fps As Integer = 0

    Private fpstimer As Stopwatch = Stopwatch.StartNew()

    Private keyupevents As New List(Of String)
    Private keydownevents As New List(Of String)
    Public mouse As MouseEventArgs
    Public mouse_left As MouseButtons = MouseButtons.Left
    Public mouse_right As MouseButtons = MouseButtons.Right
    Public mouse_middle As MouseButtons = MouseButtons.Middle

    Sub setDisplay(ByRef f As Form, resolution As String, Optional title As String = "", Optional fullscreen As Boolean = False)
        form = f
        width = CInt(resolution.Split({"x"}, StringSplitOptions.None)(0))
        height = CInt(resolution.Split({"x"}, StringSplitOptions.None)(1))

        form.Width = width
        form.Height = height
        form.Width += form.Width - form.DisplayRectangle().Width
        form.Height += form.Height - form.DisplayRectangle().Height

        form.Text = title

        form.KeyPreview = True

        If fullscreen Then
            form.FormBorderStyle = Windows.Forms.FormBorderStyle.None
            form.WindowState = FormWindowState.Maximized
        Else
            form.FormBorderStyle = Windows.Forms.FormBorderStyle.FixedSingle
            form.WindowState = FormWindowState.Normal
        End If

        displaycontext = BufferedGraphicsManager.Current
        displaybuffer = displaycontext.Allocate(form.CreateGraphics, form.DisplayRectangle)

        Windows.Forms.Cursor.Position = New Point(width / 2 + form.Location.X, height / 2 + form.Location.Y)

    End Sub

    Sub pushKeyUpEvent(key As String)
        keyupevents.Add(key)
    End Sub

    Sub pushKeyDownEvent(key As String)
        keydownevents.Add(key)
    End Sub

    Function getKeyUpEvents()
        Dim tlist As List(Of String)
        Try
            tlist = keyupevents.ToList()
        Catch ex As ArgumentException
            tlist = New List(Of String)
        End Try
        keyupevents.Clear()
        Return tlist
    End Function

    Function getKeyDownEvents()
        Dim tlist As List(Of String)
        Try
            tlist = keydownevents.ToList()
        Catch ex As ArgumentException
            tlist = New List(Of String)
        End Try
        keydownevents.Clear()
        Return tlist
    End Function

    Private Sub form_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles form.MouseMove, form.MouseDown
        mouse = e
    End Sub

    Private Sub form_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles form.KeyDown
        keydownevents.Add(e.KeyCode().ToString())
    End Sub

    Private Sub form_KeyUp(ByVal sender As Object, ByVal e As KeyEventArgs) Handles form.KeyUp
        keyupevents.Add(e.KeyCode().ToString())
    End Sub

    Sub clockTick(fps As Decimal)
        Dim tfps As Decimal
        tfps = 1000 / fps
        While fpstimer.ElapsedMilliseconds < tfps
        End While
        fpstimer.Reset()
        fpstimer.Start()
    End Sub

    Sub update()
        Try
            displaybuffer.Render()
        Catch ex As System.ArgumentException
            End
        End Try
    End Sub

    Function collideRect(rect1 As Rectangle, rect2 As Rectangle) As Boolean
        Dim collision As Boolean = False
        Dim crect As Rectangle
        crect = Rectangle.Intersect(rect1, rect2)
        If crect <> New Rectangle(0, 0, 0, 0) Then
            collision = True
        End If
        Return collision
    End Function

    Function getRect() As Rectangle
        Return New Rectangle(0, 0, width, height)
    End Function

    Function getCenter() As Point
        Return New Point(width / 2, height / 2)
    End Function

    Function getImage(path As String) As Image
        Return Image.FromFile(path)
    End Function

    Function getImageFromDisplay() As Image
        Dim bitmap As Bitmap = New Bitmap(width, height, displaybuffer.Graphics)
        Dim g As Graphics = Graphics.FromImage(bitmap)
        g.CopyFromScreen(New Point(form.Location.X + (form.Width - form.DisplayRectangle().Width) / 2, form.Location.Y + (form.Height - form.DisplayRectangle().Height) * (15 / 19)), New Point(0, 0), New Size(width, height))
        Return bitmap
    End Function

    Sub saveImage(path As String)
        'Dim Bitmap As Bitmap = New Bitmap(width, height, displaybuffer.Graphics)
        Dim bitmap As Bitmap
        bitmap = getImageFromDisplay()
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Bmp)
        'displaybuffer.Graphics.from()
    End Sub

    'drawing -----------------------------------------------------------------------
    Sub fill(color As System.Drawing.Color)
        drawRect(New Rectangle(0, 0, form.Width, form.Height), color)
    End Sub

    Sub setPixel(point As Point, color As System.Drawing.Color)
        drawRect(New Rectangle(point.X, point.Y, 1, 1), color)
    End Sub

    Sub blit(image As Image, rect As Rectangle)
        displaybuffer.Graphics.DrawImage(image, rect)
    End Sub

    Sub drawText(point As Point, s As String, color As System.Drawing.Color, Optional fontsize As Single = 16, Optional fontname As String = "Arial")
        Dim brush As New System.Drawing.SolidBrush(color)
        Dim font As New System.Drawing.Font(fontname, fontsize)
        Dim format As New System.Drawing.StringFormat
        displaybuffer.Graphics.DrawString(s, font, brush, point.X, point.Y, format)
        brush.Dispose()
    End Sub

    Sub drawCenteredText(rect As Rectangle, s As String, color As System.Drawing.Color, Optional fontsize As Single = 16, Optional fontname As String = "Arial")
        Dim font As New System.Drawing.Font(fontname, fontsize)
        Dim format As New System.Drawing.StringFormat
        TextRenderer.DrawText(displaybuffer.Graphics, s, font, rect, color, color.Empty, TextFormatFlags.VerticalCenter Or TextFormatFlags.HorizontalCenter)
    End Sub

    'line drawing ------------------------------------------------------------------
    Sub drawLines(points() As Point, color As System.Drawing.Color, Optional width As Integer = 1)
        If points.Length >= 2 Then
            Dim pen As New Pen(color, width)
            pen.Alignment = Drawing2D.PenAlignment.Center
            displaybuffer.Graphics.DrawLines(pen, points)
            pen.Dispose()
        End If
    End Sub

    Sub drawLine(point1 As Point, point2 As Point, color As System.Drawing.Color, Optional width As Integer = 1)
        Dim pen As New Pen(color, width)
        pen.Alignment = Drawing2D.PenAlignment.Center
        displaybuffer.Graphics.DrawLine(pen, point1, point2)
        pen.Dispose()
    End Sub

    'shape drawing ------------------------------------------------------------------
    Sub drawRect(rect As Rectangle, color As System.Drawing.Color, Optional filled As Boolean = True)
        Dim pen As New Pen(color)
        Dim brush As New System.Drawing.SolidBrush(color)
        If filled Then
            displaybuffer.Graphics.FillRectangle(brush, rect)
        Else
            displaybuffer.Graphics.DrawRectangle(pen, rect)
        End If
        brush.Dispose()
        pen.Dispose()
    End Sub

    Sub drawCircle(center As Point, radius As Integer, color As System.Drawing.Color, Optional filled As Boolean = True)
        Dim rect As New Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2)
        drawEllipse(rect, color, filled)
    End Sub

    Sub drawEllipse(rect As Rectangle, color As System.Drawing.Color, Optional filled As Boolean = True)
        Dim pen As New Pen(color)
        Dim brush As New System.Drawing.SolidBrush(color)
        If filled Then
            displaybuffer.Graphics.FillEllipse(brush, rect)
        Else
            displaybuffer.Graphics.DrawEllipse(pen, rect)
        End If
        brush.Dispose()
        pen.Dispose()
    End Sub

End Class

'======================================== GENERIC SPRITE CLASS ========================================
Public Class Sprite
    Public image As Image
    Public width As Decimal = 0
    Public height As Decimal = 0
    Public x As Decimal = 0
    Public y As Decimal = 0
    Public pxc As Decimal = 0
    Public nxc As Decimal = 0
    Public pyc As Decimal = 0
    Public nyc As Decimal = 0
    Public angle As Decimal = 0
    Public speed As Decimal = 0
    Public frames As Integer = 0
    Public color As System.Drawing.Color = color.White

    Public Function clone() As sprite
        Return DirectCast(Me.MemberwiseClone(), sprite)
    End Function

    Sub move(Optional trig As Boolean = False)
        Dim mp As PointF
        mp = calcMove(trig)
        x = mp.X
        y = mp.Y
    End Sub

    Function calcMove(Optional trig As Boolean = False) As PointF
        Dim xt, yt As Decimal
        If trig Then
            xt = x + Math.Cos(angle * (Math.PI / 180)) * speed
            yt = y + Math.Sin(angle * (Math.PI / 180)) * speed
        Else
            xt = x + pxc - nxc
            yt = y + pyc - nyc
        End If
        Return New PointF(xt, yt)
    End Function

    Sub normalizeAngle()
        While angle > 360
            angle -= 360
        End While
        While angle < 0
            angle += 360
        End While
    End Sub

    Function keepInBounds(bounds As Rectangle, Optional trig As Boolean = False, Optional bounce As Boolean = False)
        Dim move As PointF
        Dim wd As Boolean = False
        If Not trig Then
            move = calcMove()
            If move.X + width > bounds.X + bounds.Width Then
                wd = True
                x = bounds.X + bounds.Width - width
                pxc = 0
                If bounce Then
                    nxc = speed
                End If

            ElseIf move.X < bounds.X Then
                wd = True
                x = bounds.X
                If bounce Then
                    pxc = speed
                End If
                nxc = 0
            End If

            If move.Y + height > bounds.Y + bounds.Height Then
                wd = True
                y = bounds.Y + bounds.Height - height
                pyc = 0
                If bounce Then
                    nyc = speed
                End If

            ElseIf move.Y < bounds.Y Then
                wd = True
                y = bounds.Y
                If bounce Then
                    pyc = speed
                End If
                nyc = 0
            End If

        Else
            move = calcMove(True)
            If move.X + width > bounds.X + bounds.Width Then
                wd = True
                x = bounds.X + bounds.Width - width
                If bounce Then
                    angle = -angle + 180
                End If

            ElseIf move.X < bounds.X Then
                wd = True
                x = bounds.X
                If bounce Then
                    angle = -angle + 180
                End If

            End If
            If move.Y + height > bounds.Y + bounds.Height Then
                wd = True
                y = bounds.Y + bounds.Height - height
                If bounce Then
                    angle = -angle
                End If

            ElseIf move.Y < bounds.Y Then
                wd = True
                y = bounds.Y
                If bounce Then
                    angle = -angle
                End If
            End If
            normalizeAngle()
        End If
        Return wd
    End Function

    Sub setRect(rect As Rectangle)
        x = rect.X
        y = rect.Y
        width = rect.Width
        height = rect.Height
    End Sub

    Sub setXY(point As Point)
        x = point.X
        y = point.Y
    End Sub

    Function getRect() As Rectangle
        Return New Rectangle(x, y, width, height)
    End Function

    Function getXY() As Point
        Return New Point(x, y)
    End Function

    Function getCenter() As Point
        Return New Point(x + width / 2, y + height / 2)
    End Function

    Function getRadius() As Decimal
        Return (getRect().Width / 2 + getRect().Width / 2) / 2
    End Function

    Sub basicControls(key As String, KeyDown As Boolean)
        Dim posmod As Integer = 0
        If KeyDown Then
            posmod = speed
        End If
        If key = "W" Then
            nyc = posmod
        End If
        If key = "S" Then
            pyc = posmod
        End If
        If key = "A" Then
            nxc = posmod
        End If
        If key = "D" Then
            pxc = posmod
        End If
    End Sub

End Class

'======================================== GENERIC BUTTON CLASS ========================================
Class Button
    Public x As Decimal = 0
    Public y As Decimal = 0
    Public width As Decimal = 0
    Public height As Decimal = 0

    Public bgcolorinactive As System.Drawing.Color = Color.White
    Public bgcoloractive As System.Drawing.Color = Color.White
    Public textcolor As System.Drawing.Color = Color.Black

    Public vbgame As VBGame

    Public text As String = ""
    Public fontsize As Single = 16
    Public fontname As String = "Arial"

    Sub useDisplay(ByRef vbgamet As VBGame)
        vbgame = vbgamet
    End Sub

    Public Function clone() As button
        Return DirectCast(Me.MemberwiseClone(), button)
    End Function

    Sub setColor(inactivecolor As System.Drawing.Color, activecolor As System.Drawing.Color)
        bgcolorinactive = inactivecolor
        bgcoloractive = activecolor
    End Sub

    Sub draw(active As Boolean)
        Dim font As New Font(fontname, fontsize)
        If active Then
            vbgame.drawRect(getRect(), bgcoloractive)
        Else
            vbgame.drawRect(getRect(), bgcolorinactive)
        End If
        vbgame.drawCenteredText(getRect(), text, textcolor, fontsize, fontname)
    End Sub

    Function handle(Optional drawb As Boolean = True) As MouseButtons
        Dim buttondown As MouseButtons = MouseButtons.None
        If Not IsNothing(vbgame.mouse) Then
            If vbgame.collideRect(New Rectangle(vbgame.mouse.Location.X, vbgame.mouse.Location.Y, 1, 1), getRect()) Then
                If drawb Then
                    draw(True)
                End If
                buttondown = vbgame.mouse.Button
            Else
                If drawb Then
                    draw(False)
                End If
            End If
        Else
            If drawb Then
                draw(False)
            End If
        End If
        Return buttondown
    End Function

    Sub setRect(rect As Rectangle)
        x = rect.X
        y = rect.Y
        width = rect.Width
        height = rect.Height
    End Sub

    Sub setXY(point As Point)
        x = point.X
        y = point.Y
    End Sub

    Function getRect() As Rectangle
        Return New Rectangle(x, y, width, height)
    End Function

    Function getXY() As Point
        Return New Point(x, y)
    End Function

    Function getCenter() As Point
        Return New Point(x + width / 2, y + height / 2)
    End Function

    Function getRadius() As Decimal
        Return (getRect().Width / 2 + getRect().Width / 2) / 2
    End Function

End Class