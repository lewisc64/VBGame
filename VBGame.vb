﻿Imports System.Windows.Forms
Imports System.IO

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

Public Class DrawBase

    Public displaybuffer As BufferedGraphics
    Public displaycontext As System.Drawing.BufferedGraphicsContext

    Public x = 0
    Public y = 0
    Public width As Integer
    Public height As Integer

    ''' <summary>
    ''' Renders the display buffer to the form.
    ''' </summary>
    ''' <remarks></remarks>
    Sub update()
        Try
            displaybuffer.Render()
        Catch ex As System.ArgumentException
            End
        End Try
    End Sub

    ''' <summary>
    ''' Gets the display area as a rectangle.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function getRect() As Rectangle
        Return New Rectangle(x, y, width, height)
    End Function

    Function shiftRect(rect As Rectangle) As Rectangle
        Return New Rectangle(rect.X + x, rect.Y + y, rect.Width, rect.Height)
    End Function

    Function shiftPoint(point As Point) As Point
        Return New Point(point.X + x, point.Y + y)
    End Function

    Function getCenter() As Point
        Return New Point(width / 2, height / 2)
    End Function

    Sub fill(color As System.Drawing.Color)
        drawRect(New Rectangle(0, 0, width, height), color) 'Rect shift not needed
    End Sub

    Sub setPixel(point As Point, color As System.Drawing.Color)
        drawRect(New Rectangle(point.X + x, point.Y + y, 1, 1), color)
    End Sub

    ''' <summary>
    ''' Draws an image to the screen.
    ''' </summary>
    ''' <param name="image"></param>
    ''' <param name="rect"></param>
    ''' <remarks></remarks>
    Sub blit(image As Image, rect As Rectangle)
        If Not IsNothing(image) Then
            displaybuffer.Graphics.DrawImage(image, shiftRect(rect))
        End If
    End Sub

    Sub drawText(point As Point, s As String, color As System.Drawing.Color, Optional fontsize As Single = 16, Optional fontname As String = "Arial")
        Dim brush As New System.Drawing.SolidBrush(color)
        Dim font As New System.Drawing.Font(fontname, fontsize)
        Dim format As New System.Drawing.StringFormat
        displaybuffer.Graphics.DrawString(s, font, brush, point.X + x, point.Y + y, format)
        brush.Dispose()
    End Sub

    Sub drawCenteredText(rect As Rectangle, s As String, color As System.Drawing.Color, Optional fontsize As Single = 16, Optional fontname As String = "Arial")
        Dim font As New System.Drawing.Font(fontname, fontsize)
        TextRenderer.DrawText(displaybuffer.Graphics, s, font, shiftRect(rect), color, color.Empty, TextFormatFlags.VerticalCenter Or TextFormatFlags.HorizontalCenter)
    End Sub

    'line drawing ------------------------------------------------------------------
    Sub drawLines(ByVal points() As Point, color As System.Drawing.Color, Optional width As Integer = 1)

        If x <> 0 And y <> 0 Then
            For Each Point As Point In points
                Point = shiftPoint(Point)
            Next
        End If

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
        displaybuffer.Graphics.DrawLine(pen, shiftPoint(point1), shiftPoint(point2))
        pen.Dispose()
    End Sub

    'shape drawing ------------------------------------------------------------------
    Sub drawRect(ByVal rect As Rectangle, color As System.Drawing.Color, Optional filled As Boolean = True)
        rect = shiftRect(rect)
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
        drawEllipse(rect, color, filled) 'Rect shift not needed, ellipse takes care of that.
    End Sub

    Sub drawEllipse(rect As Rectangle, color As System.Drawing.Color, Optional filled As Boolean = True)
        rect = shiftRect(rect)
        If filled Then
            Dim brush As New System.Drawing.SolidBrush(color)
            displaybuffer.Graphics.FillEllipse(brush, New Rectangle(rect.X + x, rect.Y + y, rect.Width, rect.Height))
            brush.Dispose()
        Else
            Dim pen As New Pen(color)
            displaybuffer.Graphics.DrawEllipse(pen, rect)
            pen.Dispose()
        End If
    End Sub
End Class

''' <summary>
''' Game loop must be in a thread.
''' </summary>
''' <remarks></remarks>
Public Class VBGame
    Inherits DrawBase

    Private WithEvents form As Form

    Public Shared white = Color.FromArgb(255, 255, 255)
    Public Shared black = Color.FromArgb(0, 0, 0)
    Public Shared grey = Color.FromArgb(128, 128, 128)
    Public Shared red = Color.FromArgb(255, 0, 0)
    Public Shared green = Color.FromArgb(0, 255, 0)
    Public Shared blue = Color.FromArgb(0, 0, 255)
    Public Shared cyan = Color.FromArgb(0, 255, 255)
    Public Shared yellow = Color.FromArgb(255, 255, 0)
    Public Shared magenta = Color.FromArgb(255, 0, 255)

    Private fps As Integer = 0

    Private fpstimer As Stopwatch = Stopwatch.StartNew()

    Private keyupevents As New List(Of KeyEventArgs)
    Private keydownevents As New List(Of KeyEventArgs)

    Private mouseevents As New List(Of MouseEvent)
    Public mouse As MouseEventArgs
    Public Shared mouse_left As MouseButtons = MouseButtons.Left
    Public Shared mouse_right As MouseButtons = MouseButtons.Right
    Public Shared mouse_middle As MouseButtons = MouseButtons.Middle

    ''' <summary>
    ''' Saves image to a file
    ''' </summary>
    ''' <param name="image"></param>
    ''' <param name="path"></param>
    ''' <param name="format"></param>
    ''' <remarks></remarks>
    Public Shared Sub saveImage(image As Bitmap, path As String, Optional format As System.Drawing.Imaging.ImageFormat = Nothing)
        If IsNothing(format) Then
            format = System.Drawing.Imaging.ImageFormat.Png
        End If
        image.Save(path, format)
    End Sub

    Public Shared Function loadImage(path As String) As Image
        Return Image.FromFile(path)
    End Function

    Public Shared Function collideRect(rect1 As Rectangle, rect2 As Rectangle) As Boolean
        Return rect1.IntersectsWith(rect2)
    End Function

    ''' <summary>
    ''' Seperates images from a larger image. Operates from left to right, then moving down.
    ''' </summary>
    ''' <param name="sheet">Image of spritesheet.</param>
    ''' <param name="rowcolumn">Amount of images in the width and height.</param>
    ''' <param name="nimages">How many images from the sheet should be sliced.</param>
    ''' <param name="reverse">To reverse the individual images after slicing.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function sliceSpriteSheet(sheet As Image, rowcolumn As Size, Optional nimages As Integer = 0, Optional reverse As Boolean = False) As List(Of Image)
        Dim list As New List(Of Image)
        Dim n As Integer = 0
        Dim image As Image = New Bitmap(CInt(sheet.Width / rowcolumn.Width), CInt(sheet.Height / rowcolumn.Height))
        Dim g As Graphics = Graphics.FromImage(image)
        g.SmoothingMode = Drawing2D.SmoothingMode.None
        g.InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
        For y As Integer = 0 To sheet.Height - image.Height Step image.Height
            For x As Integer = 0 To sheet.Width - image.Width Step image.Width
                n += 1
                g.DrawImage(sheet, New Rectangle(0, 0, image.Width, image.Height), New Rectangle(x, y, image.Width, image.Height), GraphicsUnit.Pixel)
                If reverse Then
                    image.RotateFlip(RotateFlipType.RotateNoneFlipX)
                End If
                list.Add(image.Clone())
                g.Clear(Color.Empty)
                If n >= nimages And nimages <> 0 Then
                    Exit For
                End If
            Next
            If n >= nimages And nimages <> 0 Then
                Exit For
            End If
        Next
        Return list
    End Function

    ''' <summary>
    ''' Configures VBGame for operation. Must be called before starting game loop.
    ''' </summary>
    ''' <param name="f">Form that will be drawn on.</param>
    ''' <param name="resolution">Width and height of display area, in pixels.</param>
    ''' <param name="title">String that will be displayed on title bar.</param>
    ''' <param name="sharppixels">Enabling this will turn off pixel smoothing. Good for pixel art.</param>
    ''' <param name="fullscreen"></param>
    ''' <remarks></remarks>
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

    'Form event hooks.

    Private Sub form_MouseWheel(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Form.MouseWheel
        mouseevents.Add(MouseEvent.InterpretFormEvent(e, MouseEvent.actions.scroll))
        mouse = e
    End Sub

    Private Sub form_MouseMove(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Form.MouseMove
        mouseevents.Add(MouseEvent.InterpretFormEvent(e, MouseEvent.actions.move))
        mouse = e
    End Sub

    Private Sub form_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Form.MouseDown
        mouseevents.Add(MouseEvent.InterpretFormEvent(e, MouseEvent.actions.down))
        mouse = e
    End Sub

    Private Sub form_MouseClick(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Form.MouseClick
        mouseevents.Add(MouseEvent.InterpretFormEvent(e, MouseEvent.actions.up))
        mouse = e
    End Sub

    Private Sub form_MouseDoubleClick(ByVal sender As Object, ByVal e As MouseEventArgs) Handles Form.MouseDoubleClick
        mouseevents.Add(MouseEvent.InterpretFormEvent(e, MouseEvent.actions.up))
        mouse = e
    End Sub

    Private Sub form_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles Form.KeyDown
        keydownevents.Add(e)
    End Sub

    Private Sub form_KeyUp(ByVal sender As Object, ByVal e As KeyEventArgs) Handles Form.KeyUp
        keyupevents.Add(e)
    End Sub

    ''' <summary>
    ''' Waits so that the specified fps can be achieved.
    ''' </summary>
    ''' <param name="fps"></param>
    ''' <remarks></remarks>
    Sub clockTick(fps As Double)
        Dim tfps As Double
        tfps = 1000 / fps
        While fpstimer.ElapsedMilliseconds < tfps
        End While
        fpstimer.Reset()
        fpstimer.Start()
    End Sub

    ''' <summary>
    ''' Gets the time in milliseconds since the last clockTick()
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function getTime()
        Return fpstimer.ElapsedMilliseconds
    End Function

    Function getImageFromDisplay() As Image
        Dim bitmap As Bitmap = New Bitmap(width, height, displaybuffer.Graphics)
        Dim g As Graphics = Graphics.FromImage(bitmap)
        g.CopyFromScreen(New Point(form.Location.X + (form.Width - form.DisplayRectangle().Width) / 2, form.Location.Y + (form.Height - form.DisplayRectangle().Height) * (15 / 19)), New Point(0, 0), New Size(width, height))
        Return bitmap
    End Function

    'Drawing
End Class

''' <summary>
''' Gets a portion of the display given to do drawing operations on.
''' Anything drawn outside of the bounds of the surface will not be drawn on the parent display.
''' Surfaces are static.
''' </summary>
''' <remarks></remarks>
Public Class Surface
    Inherits DrawBase

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="rect"></param>
    ''' <param name="parentdisplay">Display to draw on.</param>
    ''' <remarks></remarks>
    Public Sub New(rect As Rectangle, parentdisplay As VBGame)

        x = rect.X
        y = rect.Y
        width = rect.Width
        height = rect.Height

        displaycontext = BufferedGraphicsManager.Current
        displaybuffer = displaycontext.Allocate(parentdisplay.displaybuffer.Graphics, getRect())
    End Sub

End Class

Public Class BitmapSurface
    Inherits DrawBase

    Private displaygraphics As Graphics
    Private display As Bitmap

    Public Sub New(size As Size, Optional format As Imaging.PixelFormat = Nothing)

        If format = Imaging.PixelFormat.Undefined Then
            format = Imaging.PixelFormat.Format24bppRgb
        End If

        width = size.Width
        height = size.Height

        display = New Bitmap(width, height, format)
        displaygraphics = Graphics.FromImage(display)

        displaycontext = BufferedGraphicsManager.Current
        displaybuffer = displaycontext.Allocate(displaygraphics, getRect())
    End Sub

    Public Function getImage(Optional autoupdate = True)
        If autoupdate Then
            update()
        End If
        Return display
    End Function

End Class

Public Class Sound

    Public Declare Function mciSendString Lib "winmm.dll" Alias "mciSendStringA" (ByVal lpstrCommand As String, ByVal lpstrReturnString As String, ByVal uReturnLength As Integer, ByVal hwndCallback As Integer) As Integer

    Public name As String
    Private vol As Integer = 1000

    Public Sub New(filename As String)
        name = filename
    End Sub

    ''' <summary>
    ''' Changing this will update the volume.
    ''' </summary>
    ''' <value></value>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Property volume
        Set(value)
            vol = value
            If vol < 0 Then
                vol = 0
            End If
            If vol > 1000 Then
                vol = 1000
            End If
            setVolume(vol)
        End Set
        Get
            Return vol
        End Get
    End Property

    Sub load()
        mciSendString("Open " & getPath() & " alias " & name, CStr(0), 0, 0)
    End Sub

    ''' <summary>
    ''' </summary>
    ''' <param name="repeat">If enabled, the sound will loop. Note: this does not work with .wav files.</param>
    ''' <remarks></remarks>
    Sub play(Optional repeat As Boolean = False)
        If repeat Then
            load()
            mciSendString("play " & name & " repeat", CStr(0), 0, 0)
        Else
            load()
            mciSendString("play " & name, CStr(0), 0, 0)
        End If
    End Sub

    Sub halt()
        mciSendString("close " & name, CStr(0), 0, 0)
    End Sub

    Sub pause()
        mciSendString("pause " & name, CStr(0), 0, 0)
    End Sub

    Private Sub setVolume(volume As Integer)
        mciSendString("setaudio " & name & " volume to " & volume, CStr(0), 0, 0)
    End Sub

    Private Function getPath() As String
        Return Directory.GetCurrentDirectory() & "\" & name
    End Function

End Class

Public Class Animation

    Public frames As New List(Of Image)
    Public interval As Integer 'time between frames (ms)
    Public index As Integer 'current frame

    Public playing As Boolean

    Public loopanim As Boolean = True

    Public timer As New Stopwatch

    Public Function clone() As Animation
        Return DirectCast(Me.MemberwiseClone(), Animation)
    End Function

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

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="strip">Image of spritesheet.</param>
    ''' <param name="rowcolumn">Amount of images in the width and height.</param>
    ''' <param name="nframes">How many images from the sheet should be sliced.</param>
    ''' <param name="reverse">To reverse the individual images after slicing.</param>
    ''' <param name="animloop">If enabled, the animation will loop.</param>
    ''' <remarks></remarks>
    Sub New(strip As Image, rowcolumn As Size, timing As Integer, Optional nframes As Integer = 0, Optional reverse As Boolean = False, Optional animloop As Boolean = True)
        loopanim = animloop
        index = 0
        interval = timing
        getFramesFromStrip(strip, rowcolumn, nframes, reverse)
        playing = False
    End Sub

    ''' <summary>
    ''' See VBGame.sliceSpriteSheet()
    ''' </summary>
    ''' <remarks></remarks>
    Sub getFramesFromStrip(strip As Image, rowcolumn As Size, Optional nframes As Integer = 0, Optional reverse As Boolean = False)
        frames = VBGame.sliceSpriteSheet(strip, rowcolumn, nframes, reverse)
    End Sub

    ''' <summary>
    ''' Used in conjection with VBGame.blit(), this will pick the image to return based on a timer.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function handle() As Image
        While timer.ElapsedMilliseconds >= interval
            timer.Restart()
            Return getFrame(loopanim)
        End While
        Return frames(index)
    End Function

    ''' <summary>
    ''' Gets the next frame.
    ''' </summary>
    ''' <param name="loopanim"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
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

    Public Function clone() As Animations
        Return DirectCast(Me.MemberwiseClone(), Animations)
    End Function

    Sub addAnim(key As String, animation As Animation)
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

    ''' <summary>
    ''' Returns a frame from the active animation.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function handle() As Image
        Return getAnim(active).handle()
    End Function

    Function getAnim(key As String) As Animation
        Return items(key)
    End Function

    Sub playActive()
        getActive().playAnim()
    End Sub

    Sub stopActive()
        getActive().stopAnim()
    End Sub

    Sub pauseActive()
        getActive().pauseAnim()
    End Sub

    Function getActive() As Animation
        Return items(active)
    End Function

End Class

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

    ''' <summary>
    ''' Ensures the sprite's angle is between 0 and 360 degrees.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub normalizeAngle()
        While angle > 360
            angle -= 360
        End While
        While angle < 0
            angle += 360
        End While
    End Sub

    ''' <summary>
    ''' Keeps the sprite in a rectangle.
    ''' </summary>
    ''' <param name="bounds">Rectangle container.</param>
    ''' <param name="trig">Whether or not the sprite is using angled movement.</param>
    ''' <param name="bounce">If enabled, the sprite will change it's movement to give the appearence of bouncing.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
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
End Class

Class Button

    Inherits Sprite

    ''' <summary>
    ''' Put in vbgame.getMouseEvents() loop.
    ''' </summary>
    ''' <remarks></remarks>

    Public display As VBGame
    Public hover As Boolean = False
    Public hovercolor As Color
    Public hoverimage As Image

    Public text As String
    Public hovertext As String
    Public fontsize As Integer
    Public fontname As String
    Public textcolor As System.Drawing.Color
    Public hovertextcolor As System.Drawing.Color

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="vbgamet">Display to draw onto.</param>
    ''' <param name="textt">Text to display on the button.</param>
    ''' <param name="rect">Rectangle of the button</param>
    ''' <param name="fontnamet"></param>
    ''' <param name="fontsizet"></param>
    ''' <remarks></remarks>
    Public Sub New(ByRef vbgame As VBGame, textt As String, Optional rect As Rectangle = Nothing, Optional fontnamet As String = "Arial", Optional fontsizet As Integer = 0)
        display = vbgame
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

    ''' <summary>
    ''' Calculates the font size based on the current rectangle.
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub calculateFontSize()
        For f As Integer = 1 To 75
            If display.displaybuffer.Graphics.MeasureString(text, New Font(fontname, f)).Width < width Then
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

    ''' <summary>
    ''' Draws the button. Keep out of event loops.
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub draw()
        If IsNothing(image) Then
            If hover Then
                display.drawRect(getRect(), hovercolor)
            Else
                display.drawRect(getRect(), color)
            End If
        Else
            If hover Then
                display.blit(hoverimage, getRect())
            Else
                display.blit(image, getRect())
            End If
        End If

        If hover Then
            If IsNothing(hovertext) Then
                display.drawCenteredText(getRect(), text, hovertextcolor, fontsize, fontname)
            Else
                display.drawCenteredText(getRect(), hovertext, hovertextcolor, fontsize, fontname)
            End If
        Else
            display.drawCenteredText(getRect(), text, textcolor, fontsize, fontname)
        End If

    End Sub

    ''' <summary>
    ''' Put inside the VBGame.getMouseEvents() loop. Will return the MouseEvent of a successful click.
    ''' </summary>
    ''' <param name="e">MouseEvent from loop.</param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function handle(e As MouseEvent)
        If display.collideRect(New Rectangle(e.location.X, e.location.Y, 1, 1), getRect()) Then
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