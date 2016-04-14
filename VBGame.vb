
Imports System.Windows.Forms


Public Class MouseEvent

    Enum buttons
        none
        left
        right
        middle
        scrollUp
        scrollDown
    End Enum

    Enum actions
        move
        down
        up
        scroll
    End Enum

    Public action
    Public location As Point
    Public button

    Public Sub New(locationt As Point, actiont As Byte, buttont As Byte)
        action = actiont
        location = locationt
        button = buttont
    End Sub

    Public Shared Function InterpretFormEvent(e As MouseEventArgs, action As Byte)
        Dim button As Byte
        If action = actions.down Or action = actions.up Then
            If e.Button = MouseButtons.Left Then
                button = buttons.left
            ElseIf e.Button = MouseButtons.Right Then
                button = buttons.right
            ElseIf e.Button = MouseButtons.Middle Then
                button = buttons.middle
            End If
        ElseIf action = actions.scroll Then
            If e.Delta > 0 Then
                button = buttons.scrollUp
            ElseIf e.Delta < 0 Then
                button = buttons.scrollDown
            End If
        End If
        Return New MouseEvent(e.Location, action, button)
    End Function

End Class

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
    ''' <remarks>Version 0.9</remarks>

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

    Private keyupevents As New List(Of KeyEventArgs)
    Private keydownevents As New List(Of KeyEventArgs)

    Private mouseevents As New List(Of MouseEvent)
    Public mouse As MouseEventArgs
    Public mouse_left As MouseButtons = MouseButtons.Left
    Public mouse_right As MouseButtons = MouseButtons.Right
    Public mouse_middle As MouseButtons = MouseButtons.Middle

    Sub setDisplay(ByRef f As Form, resolution As Size, Optional title As String = "", Optional sharppixels As Boolean = False, Optional fullscreen As Boolean = False)
        form = f

        setSize(resolution)

        form.Invoke(Sub() form.Text = title)

        form.Invoke(Sub() form.KeyPreview = True)

        If fullscreen Then
            form.Invoke(Sub() form.FormBorderStyle = Windows.Forms.FormBorderStyle.None)
            form.Invoke(Sub() form.WindowState = FormWindowState.Maximized)
        Else
            form.Invoke(Sub() form.FormBorderStyle = Windows.Forms.FormBorderStyle.FixedSingle)
            form.Invoke(Sub() form.WindowState = FormWindowState.Normal)
        End If

        displaycontext = BufferedGraphicsManager.Current
        displaybuffer = displaycontext.Allocate(form.CreateGraphics, form.DisplayRectangle)
        If sharppixels Then
            displaybuffer.Graphics.SmoothingMode = Drawing2D.SmoothingMode.None
            displaybuffer.Graphics.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        End If

    End Sub

    Sub setSize(size As Size)
        width = size.Width
        height = size.Height
        form.Invoke(Sub() form.Width = width)
        form.Invoke(Sub() form.Height = height)
        form.Invoke(Sub() form.Width += form.Width - form.DisplayRectangle().Width)
        form.Invoke(Sub() form.Height += form.Height - form.DisplayRectangle().Height)
    End Sub

    Sub pushKeyUpEvent(key As KeyEventArgs)
        keyupevents.Add(key)
    End Sub

    Sub pushKeyDownEvent(key As KeyEventArgs)
        keydownevents.Add(key)
    End Sub

    Sub pushMouseEvent(e As MouseEvent)
        mouseevents.Add(e)
    End Sub

    Function getKeyUpEvents()
        Dim tlist As List(Of KeyEventArgs)
        Try
            tlist = keyupevents.ToList()
        Catch ex As ArgumentException
            tlist = New List(Of KeyEventArgs)
        End Try
        keyupevents.Clear()
        Return tlist
    End Function

    Function getKeyDownEvents()
        Dim tlist As List(Of KeyEventArgs)
        Try
            tlist = keydownevents.ToList()
        Catch ex As ArgumentException
            tlist = New List(Of KeyEventArgs)
        End Try
        keydownevents.Clear()
        Return tlist
    End Function

    Function getMouseEvents()
        Dim tlist As List(Of MouseEvent)
        Try
            tlist = mouseevents.ToList()
        Catch ex As ArgumentException
            tlist = New List(Of MouseEvent)
        End Try
        mouseevents.Clear()
        Return tlist
    End Function

    Private Sub form_MouseWheel(ByVal sender As Object, ByVal e As MouseEventArgs) Handles form.MouseWheel
        mouseevents.Add(MouseEvent.InterpretFormEvent(e, MouseEvent.actions.scroll))
        mouse = e
    End Sub

    Private Sub form_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles form.MouseMove
        mouseevents.Add(MouseEvent.InterpretFormEvent(e, MouseEvent.actions.move))
        mouse = e
    End Sub

    Private Sub form_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles form.MouseDown
        mouseevents.Add(MouseEvent.InterpretFormEvent(e, MouseEvent.actions.down))
        mouse = e
    End Sub

    Private Sub form_MouseClick(ByVal sender As Object, ByVal e As MouseEventArgs) Handles form.MouseClick
        mouseevents.Add(MouseEvent.InterpretFormEvent(e, MouseEvent.actions.up))
        mouse = e
    End Sub

    Private Sub form_MouseDoubleClick(ByVal sender As Object, ByVal e As MouseEventArgs) Handles form.MouseDoubleClick
        mouseevents.Add(MouseEvent.InterpretFormEvent(e, MouseEvent.actions.up))
        mouse = e
    End Sub

    Private Sub form_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles form.KeyDown
        keydownevents.Add(e)
    End Sub

    Private Sub form_KeyUp(ByVal sender As Object, ByVal e As KeyEventArgs) Handles form.KeyUp
        keyupevents.Add(e)
    End Sub

    Sub clockTick(fps As Double)
        Dim tfps As Double
        tfps = 1000 / fps
        While fpstimer.ElapsedMilliseconds < tfps
        End While
        fpstimer.Reset()
        fpstimer.Start()
    End Sub

    Function getTime()
        Return fpstimer.ElapsedMilliseconds
    End Function

    Sub update()
        Try
            displaybuffer.Render()
        Catch ex As System.ArgumentException
            End
        End Try
    End Sub

    Function collideRect(rect1 As Rectangle, rect2 As Rectangle) As Boolean
        Return rect1.IntersectsWith(rect2)
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

    Sub saveImage(image As Bitmap, path As String, Optional format As System.Drawing.Imaging.ImageFormat = Nothing)
        If IsNothing(format) Then
            format = System.Drawing.Imaging.ImageFormat.Png
        End If
        image.Save(path, format)
    End Sub

    'drawing -----------------------------------------------------------------------
    Sub fill(color As System.Drawing.Color)
        drawRect(New Rectangle(0, 0, form.Width, form.Height), color)
    End Sub

    Sub setPixel(point As Point, color As System.Drawing.Color)
        drawRect(New Rectangle(point.X, point.Y, 1, 1), color)
    End Sub

    Sub blit(image As Image, rect As Rectangle)
        If Not IsNothing(image) Then
            displaybuffer.Graphics.DrawImage(image, rect)
        End If
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
        If filled Then
            Dim brush As New System.Drawing.SolidBrush(color)
            displaybuffer.Graphics.FillRectangle(brush, rect)
            brush.Dispose()
        Else
            Dim pen As New Pen(color)
            displaybuffer.Graphics.DrawRectangle(pen, rect)
            pen.Dispose()
        End If
    End Sub

    Sub drawCircle(center As Point, radius As Integer, color As System.Drawing.Color, Optional filled As Boolean = True)
        Dim rect As New Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2)
        drawEllipse(rect, color, filled)
    End Sub

    Sub drawEllipse(rect As Rectangle, color As System.Drawing.Color, Optional filled As Boolean = True)
        If filled Then
            Dim brush As New System.Drawing.SolidBrush(color)
            displaybuffer.Graphics.FillEllipse(brush, rect)
            brush.Dispose()
        Else
            Dim pen As New Pen(color)
            displaybuffer.Graphics.DrawEllipse(pen, rect)
            pen.Dispose()
        End If
    End Sub

End Class

Public Class Animation

    Public frames As New List(Of Image)
    Public interval As Integer 'time between frames (ms)
    Public index As Integer 'current frame

    Public playing As Boolean

    Public loopanim As Boolean = True

    Public timer As New Stopwatch

    Sub playAnim()
        playing = True
        timer.Start()
    End Sub

    Sub stopAnim()
        playing = False
        timer.Reset()
        index = 0
    End Sub

    Sub pauseAnim()
        playing = False
        timer.Stop()
    End Sub

    Sub New(strip As Image, rowcolumn As Size, timing As Integer, Optional nframes As Integer = 0, Optional reverse As Boolean = False, Optional animloop As Boolean = True)
        loopanim = animloop
        index = 0
        interval = timing
        getFramesFromStrip(strip, rowcolumn, nframes, reverse)
        playing = False
    End Sub

    Sub getFramesFromStrip(strip As Image, rowcolumn As Size, Optional nframes As Integer = 0, Optional reverse As Boolean = False)
        Dim n As Integer = 0
        Dim frame As Image = New Bitmap(CInt(strip.Width / rowcolumn.Width), CInt(strip.Height / rowcolumn.Height))
        Dim g As Graphics = Graphics.FromImage(frame)
        g.SmoothingMode = Drawing2D.SmoothingMode.None
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        For y As Integer = 0 To strip.Height - frame.Height Step frame.Height
            For x As Integer = 0 To strip.Width - frame.Width Step frame.Width
                n += 1
                g.DrawImage(strip, New Rectangle(0, 0, frame.Width, frame.Height), New Rectangle(x, y, frame.Width, frame.Height), GraphicsUnit.Pixel)
                If reverse Then
                    frame.RotateFlip(RotateFlipType.RotateNoneFlipX)
                End If
                frames.Add(frame.Clone())
                g.Clear(Color.Empty)
                If n >= nframes And nframes <> 0 Then
                    Exit For
                End If
            Next
            If n >= nframes And nframes <> 0 Then
                Exit For
            End If
        Next
    End Sub

    Function handle() As Image
        If timer.ElapsedMilliseconds >= interval Then
            timer.Restart()
            Return getFrame(loopanim)
        End If
        Return frames(index)
    End Function

    Function getFrame(Optional loopanim As Boolean = True) As Image
        Dim frame As Image
        frame = frames(index)
        index += 1
        If index >= frames.ToArray.Length Then
            If loopanim Then
                index = 0
            Else
                index -= 1
                playing = False
            End If
        End If
        Return frame
    End Function

End Class

Public Class Animations

    Private items As New Dictionary(Of String, Animation)

    Public active As String

    Sub addAnim(key As String, ByRef animation As Animation)
        items.Add(key, animation)
        If IsNothing(active) Then
            active = key
        End If
    End Sub

    Sub setActive(key As String, Optional autoplay As Boolean = True)
        If active <> key Then
            getAnim(active).stopAnim()
            active = key
            If autoplay Then
                getAnim(active).playAnim()
            End If
        End If
    End Sub

    Function handle() As Image
        Return getAnim(active).handle()
    End Function

    Function getAnim(key As String) As Animation
        Return items(key)
    End Function

End Class

'======================================== GENERIC SPRITE CLASS ========================================
Public Class Sprite
    Public image As Image
    Public width As Double = 0
    Public height As Double = 0
    Public x As Double = 0
    Public y As Double = 0
    Public pxc As Double = 0
    Public nxc As Double = 0
    Public pyc As Double = 0
    Public nyc As Double = 0
    Public angle As Double = 0
    Public speed As Double = 0
    Public frames As Integer = 0
    Public color As System.Drawing.Color = color.White

    Public animations As New Animations

    Public Function clone() As Sprite
        Return DirectCast(Me.MemberwiseClone(), Sprite)
    End Function

    Public Sub New(Optional rect As Rectangle = Nothing)
        If Not IsNothing(rect) Then
            setRect(rect)
        End If
    End Sub

    Sub move(Optional trig As Boolean = False)
        Dim mp As PointF
        mp = calcMove(trig)
        x = mp.X
        y = mp.Y
    End Sub

    Function calcMove(Optional trig As Boolean = False) As PointF
        Dim xt, yt As Double
        If trig Then
            xt = x + Math.Cos(angle * (Math.PI / 180)) * speed
            yt = y + Math.Sin(angle * (Math.PI / 180)) * speed
        Else
            xt = x + pxc - nxc
            yt = y + pyc - nyc
        End If
        Return New PointF(xt, yt)
    End Function

    Private Sub normalizeAngle()
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

    Function getRadius() As Double
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

    Inherits Sprite

    ''' <summary>
    ''' Put in vbgame.getMouseEvents() loop.
    ''' </summary>
    ''' <remarks></remarks>

    Public vbgame As VBGame
    Public hover As Boolean = False
    Public hovercolor As Color
    Public hoverimage As Image

    Public text As String
    Public hovertext As String
    Public fontsize As Integer
    Public fontname As String
    Public textcolor As System.Drawing.Color
    Public hovertextcolor As System.Drawing.Color

    Public Sub New(ByRef vbgamet As VBGame, textt As String, Optional rect As Rectangle = Nothing, Optional fontnamet As String = "Arial", Optional fontsizet As Integer = 0)
        vbgame = vbgamet
        If Not IsNothing(rect) Then
            setRect(rect)
        End If
        text = textt
        fontname = fontnamet
        If fontsizet = 0 Then
            calculateFontSize()
        Else
            fontsize = fontsizet
        End If
    End Sub

    Public Sub calculateFontSize()
        For f As Integer = 1 To 75
            If vbgame.displaybuffer.Graphics.MeasureString(text, New Font(fontname, f)).Width < width Then
                fontsize = f
            End If
        Next
    End Sub

    Public Sub setColor(mouseoff As System.Drawing.Color, mouseon As System.Drawing.Color)
        color = mouseoff
        hovercolor = mouseon
    End Sub

    Public Sub setTextColor(mouseoff As System.Drawing.Color, mouseon As System.Drawing.Color)
        textcolor = mouseoff
        hovertextcolor = mouseon
    End Sub

    Public Sub draw()
        If IsNothing(image) Then
            If hover Then
                vbgame.drawRect(getRect(), hovercolor)
            Else
                vbgame.drawRect(getRect(), color)
            End If
        Else
            If hover Then
                vbgame.blit(hoverimage, getRect())
            Else
                vbgame.blit(image, getRect())
            End If
        End If

        If hover Then
            If IsNothing(hovertext) Then
                vbgame.drawCenteredText(getRect(), text, hovertextcolor, fontsize, fontname)
            Else
                vbgame.drawCenteredText(getRect(), hovertext, hovertextcolor, fontsize, fontname)
            End If
        Else
            vbgame.drawCenteredText(getRect(), text, textcolor, fontsize, fontname)
        End If

    End Sub

    Public Function handle(e As MouseEvent)
        If vbgame.collideRect(New Rectangle(e.location.X, e.location.Y, 1, 1), getRect()) Then
            hover = True
            If e.action = MouseEvent.actions.up Then
                Return e.button
            End If
        Else
            hover = False
        End If
        Return MouseEvent.buttons.none
    End Function

End Class
