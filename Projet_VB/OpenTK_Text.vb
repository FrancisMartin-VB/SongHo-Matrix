Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports System.Reflection
Imports System.Drawing.Text

Namespace Texte
    Friend Class ObjectPool(Of T As {IPoolableType(Of T), New})
        Private pool As Queue(Of T) = New Queue(Of T)()

        Public Sub New()
        End Sub

        Friend Function Acquire() As T
            Dim item As T

            If pool.Count > 0 Then
                item = pool.Dequeue()
                item.OnAcquire()
            Else
                item = New T With {.Owner = Me}
                item.OnAcquire()
            End If

            Return item
        End Function

        Friend Sub Release(item As T)
            If item Is Nothing Then Throw New ArgumentNullException("item")
            item.OnRelease()
            pool.Enqueue(item)
        End Sub
    End Class

    Friend Class TexturePacker(Of T As IPackable(Of T))
        Private root As Node
        Friend Sub New(width As Integer, height As Integer)
            If width <= 0 Then Throw New ArgumentOutOfRangeException("width", width, "Must be greater than zero.")
            If height <= 0 Then Throw New ArgumentOutOfRangeException("height", height, "Must be greater than zero.")
            root = New Node With {.Rect = New Rectangle(0, 0, width, width)}
        End Sub
#Region "--- Public Methods ---"
        ' Packs the given item into the free space of the TexturePacker. Returns the Rectangle of the packed item.
        Friend Sub Add(item As T, ByRef rect As Rectangle)
            If item.Width > root.Rect.Width OrElse item.Height > root.Rect.Height Then Throw New InvalidOperationException("The item is too large for this TexturePacker")
            Dim node As Node

            'if (!items.ContainsKey(item))
            If True Then
                node = root.Insert(item)

                ' Tree is full and insertion failed:
                If node Is Nothing Then Throw New TexturePackerFullException()

                'items.Add(item, node);
                rect = node.Rect
            End If
            'throw new ArgumentException("The item already exists in the TexturePacker.", "item");
        End Sub
        ''' <summary>  Discards all packed items. </summary>
        Friend Sub Clear()
            'items.Clear();
            root.Clear()
        End Sub

        ''' <summary> Changes the dimensions of the TexturePacker surface. </summary>
        ''' <param name="new_width">The new width of the TexturePacker surface.</param>
        ''' <param name="new_height">The new height of the TexturePacker surface.</param>
        ''' <remarks>Changing the size of the TexturePacker surface will implicitly call TexturePacker.Clear().</remarks>
        ''' <seealso cref="Clear"/>
        Friend Sub ChangeSize(new_width As Integer, new_height As Integer)
            Throw New NotImplementedException()
        End Sub
#End Region
#Region "Node"
        Friend Class Node
            Friend Sub New()
            End Sub

            Private leftField, rightField As Node
            Private rectField As Rectangle
            Private use_count As Integer

            Friend Property Rect As Rectangle
                Get
                    Return rectField
                End Get
                Set(value As Rectangle)
                    rectField = value
                End Set
            End Property

            Friend Property Left As Node
                Get
                    Return leftField
                End Get
                Set(value As Node)
                    leftField = value
                End Set
            End Property

            Friend Property Right As Node
                Get
                    Return rightField
                End Get
                Set(value As Node)
                    rightField = value
                End Set
            End Property

            Friend ReadOnly Property Leaf As Boolean
                Get
                    Return leftField Is Nothing AndAlso rightField Is Nothing
                End Get
            End Property

            Friend Function Insert(item As T) As Node
                If Not Leaf Then
                    ' Recurse towards left child, and if that fails, towards the right.
                    Dim new_node As Node = leftField.Insert(item)
                    Return If(new_node, rightField.Insert(item))
                Else
                    ' We have recursed to a leaf.

                    ' If it is not empty go back.
                    If use_count <> 0 Then Return Nothing

                    ' If this leaf is too small go back.
                    If rectField.Width < item.Width OrElse rectField.Height < item.Height Then Return Nothing


                    ' If this leaf is the right size, insert here.
                    If rectField.Width = item.Width AndAlso rectField.Height = item.Height Then
                        use_count = 1
                        Return Me
                    End If


                    ' This leaf is too large, split it up. We'll decide which way to split
                    ' by checking the width and height difference between this rectangle and
                    ' out item's bounding box. If the width difference is larger, we'll split
                    ' horizontaly, else verticaly.
                    leftField = New Node()
                    rightField = New Node()
                    Dim dw As Integer = rectField.Width - item.Width + 1
                    Dim dh As Integer = rectField.Height - item.Height + 1

                    If dw > dh Then
                        leftField.rectField = New Rectangle(rectField.Left, rectField.Top, item.Width, rectField.Height)
                        rightField.rectField = New Rectangle(rectField.Left + item.Width, rectField.Top, rectField.Width - item.Width, rectField.Height)
                    Else
                        leftField.rectField = New Rectangle(rectField.Left, rectField.Top, rectField.Width, item.Height)
                        rightField.rectField = New Rectangle(rectField.Left, rectField.Top + item.Height, rectField.Width, rectField.Height - item.Height)
                    End If

                    Return leftField.Insert(item)
                End If
            End Function

            Friend Sub Clear()
                If leftField IsNot Nothing Then leftField.Clear()
                If rightField IsNot Nothing Then rightField.Clear()
                leftField = Nothing
                rightField = Nothing
            End Sub
        End Class
#End Region
    End Class

    Friend Class TexturePackerFullException
        Inherits Exception

        Public Sub New()
            MyBase.New("There is not enough space to add this item. Consider calling the Clear() method.")
        End Sub
    End Class

    ''' <summary> Provides methods to perform layout and print hardware accelerated text. </summary>
    Friend NotInheritable Class TexteGL
        Implements ITextPrinter
#Region "Fields"
        Private glyph_rasterizer As IGlyphRasterizer
        Private text_output As ITextOutputProvider
        Private ReadOnly text_quality As TextQuality
        Private disposed As Boolean
#End Region
#Region "Constructors"
        ''' <summary>Constructs a new TextPrinter instance.</summary>
        Friend Sub New()
            Me.New(Nothing, Nothing, TextQuality.Default)
        End Sub
        ''' <summary>Constructs a new TextPrinter instance with the specified TextQuality level.</summary>
        ''' <param name="quality">The desired TextQuality of this TextPrinter.</param>
        Friend Sub New(quality As TextQuality)
            Me.New(Nothing, Nothing, quality)
        End Sub

        Private Sub New(rasterizer As IGlyphRasterizer, output As ITextOutputProvider, quality As TextQuality)
            glyph_rasterizer = rasterizer
            text_output = output
            text_quality = quality
        End Sub
#End Region
#Region "ITextPrinter Members"
#Region "Print"
        ''' <summary>
        ''' Prints text using the specified color and layout options.
        ''' </summary>
        ''' <param name="text">The System.String to print.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to print text.</param>
        ''' <param name="color">The System.Drawing.Color that will be used to print text.</param>
        Friend Sub Ecrire(text As String, font As Font, color As Color) Implements ITextPrinter.Ecrire
            Ecrire(text, font, color, RectangleF.Empty, TextPrinterOptions.Default, TextAlignment.Near, TextDirection.LeftToRight)
        End Sub
        ''' <summary>
        ''' Prints text using the specified color and layout options.
        ''' </summary>
        ''' <param name="text">The System.String to print.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to print text.</param>
        ''' <param name="color">The System.Drawing.Color that will be used to print text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        Friend Sub Ecrire(text As String, font As Font, color As Color, rect As RectangleF) Implements ITextPrinter.Ecrire
            Ecrire(text, font, color, rect, TextPrinterOptions.Default, TextAlignment.Near, TextDirection.LeftToRight)
        End Sub
        ''' <summary>
        ''' Prints text using the specified color and layout options.
        ''' </summary>
        ''' <param name="text">The System.String to print.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to print text.</param>
        ''' <param name="color">The System.Drawing.Color that will be used to print text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to print text.</param>
        Friend Sub Ecrire(text As String, font As Font, color As Color, rect As RectangleF, options As TextPrinterOptions) Implements ITextPrinter.Ecrire
            Ecrire(text, font, color, rect, options, TextAlignment.Near, TextDirection.LeftToRight)
        End Sub
        ''' <summary>
        ''' Prints text using the specified color and layout options.
        ''' </summary>
        ''' <param name="text">The System.String to print.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to print text.</param>
        ''' <param name="color">The System.Drawing.Color that will be used to print text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to print text.</param>
        ''' <param name="alignment">The OpenTK.Graphics.TextAlignment that will be used to print text.</param>
        Friend Sub Ecrire(text As String, font As Font, color As Color, rect As RectangleF, options As TextPrinterOptions, alignment As TextAlignment) Implements ITextPrinter.Ecrire
            Ecrire(text, font, color, rect, options, alignment, TextDirection.LeftToRight)
        End Sub
        ''' <summary>
        ''' Prints text using the specified color and layout options.
        ''' </summary>
        ''' <param name="text">The System.String to print.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to print text.</param>
        ''' <param name="color">The System.Drawing.Color that will be used to print text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to print text.</param>
        ''' <param name="alignment">The OpenTK.Graphics.TextAlignment that will be used to print text.</param>
        ''' <param name="direction">The OpenTK.Graphics.TextDirection that will be used to print text.</param>
        Friend Sub Ecrire(text As String, font As Font, color As Color, rect As RectangleF, options As TextPrinterOptions,
                          alignment As TextAlignment, direction As TextDirection) Implements ITextPrinter.Ecrire
            If disposed Then Throw New ObjectDisposedException([GetType]().ToString())
            If Not ValidateParameters(text, font, rect) Then Return
            Dim block As TextBlock = New TextBlock(text, font, rect, options, alignment, direction)
            TextOutput.Print(block, color, Rasterizer)
        End Sub
#End Region
#Region "Measure"
        ''' <summary> Measures text using the specified layout options. </summary>
        ''' <param name="text">The System.String to measure.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to measure text.</param>
        ''' <returns>An OpenTK.Graphics.TextExtents instance that contains the results of the measurement.</returns>
        Friend Function Mesurer(text As String, font As Font) As TextExtents Implements ITextPrinter.Mesurer
            Return Mesurer(text, font, RectangleF.Empty, TextPrinterOptions.Default, TextAlignment.Near, TextDirection.LeftToRight)
        End Function

        ''' <summary>
        ''' Measures text using the specified layout options.
        ''' </summary>
        ''' <param name="text">The System.String to measure.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to measure text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <returns>An OpenTK.Graphics.TextExtents instance that contains the results of the measurement.</returns>
        Friend Function Mesurer(text As String, font As Font, rect As RectangleF) As TextExtents Implements ITextPrinter.Mesurer
            Return Mesurer(text, font, rect, TextPrinterOptions.Default, TextAlignment.Near, TextDirection.LeftToRight)
        End Function

        ''' <summary> Measures text using the specified layout options. </summary>
        ''' <param name="text">The System.String to measure.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to measure text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to measure text.</param>
        ''' <returns>An OpenTK.Graphics.TextExtents instance that contains the results of the measurement.</returns>
        Friend Function Mesurer(text As String, font As Font, rect As RectangleF, options As TextPrinterOptions) As TextExtents Implements ITextPrinter.Mesurer
            Return Mesurer(text, font, rect, options, TextAlignment.Near, TextDirection.LeftToRight)
        End Function

        ''' <summary> Measures text using the specified layout options. </summary>
        ''' <param name="text">The System.String to measure.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to measure text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to measure text.</param>
        ''' <param name="alignment">The OpenTK.Graphics.TextAlignment that will be used to measure text.</param>
        ''' <returns>An OpenTK.Graphics.TextExtents instance that contains the results of the measurement.</returns>
        Friend Function Mesurer(text As String, font As Font, rect As RectangleF, options As TextPrinterOptions,
                                alignment As TextAlignment) As TextExtents Implements ITextPrinter.Mesurer
            Return Mesurer(text, font, rect, options, alignment, TextDirection.LeftToRight)
        End Function

        ''' <summary> Measures text using the specified layout options. </summary>
        ''' <param name="text">The System.String to measure.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to measure text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to measure text.</param>
        ''' <param name="alignment">The OpenTK.Graphics.TextAlignment that will be used to measure text.</param>
        ''' <param name="direction">The OpenTK.Graphics.TextDirection that will be used to measure text.</param>
        ''' <returns>An OpenTK.Graphics.TextExtents instance that contains the results of the measurement.</returns>
        Friend Function Mesurer(text As String, font As Font, rect As RectangleF, options As TextPrinterOptions,
                                 alignment As TextAlignment, direction As TextDirection) As TextExtents Implements ITextPrinter.Mesurer
            If disposed Then Throw New ObjectDisposedException([GetType]().ToString())
            If Not ValidateParameters(text, font, rect) Then Return TextExtents.Empty
            Dim block As TextBlock = New TextBlock(text, font, rect, options, alignment, direction)
            Return Rasterizer.MeasureText(block)
        End Function
#End Region
        Friend Sub Clear()
            If disposed Then Throw New ObjectDisposedException([GetType]().ToString())
            TextOutput.Clear()
            Rasterizer.Clear()
        End Sub
        ''' <summary> Sets up a resolution-dependent orthographic projection. </summary>
        Public Sub Begin() Implements ITextPrinter.Begin
            TextOutput.Begin()
        End Sub
        ''' <summary> Restores the projection and modelview matrices to their previous state.
        ''' </summary>
        Public Sub [End]() Implements ITextPrinter.End
            TextOutput.End()
        End Sub
#End Region
#Region "Private Members"
        Private ReadOnly Property Rasterizer As IGlyphRasterizer
            Get
                If glyph_rasterizer Is Nothing Then glyph_rasterizer = New GdiPlusGlyphRasterizer()
                Return glyph_rasterizer
            End Get
        End Property

        Private ReadOnly Property TextOutput As ITextOutputProvider
            Get
                If text_output Is Nothing Then text_output = GL1TextOutputProvider.Create(text_quality)
                Return text_output
            End Get
        End Property
#End Region
        Private Shared Function ValidateParameters(text As String, font As Font, rect As RectangleF) As Boolean
            If String.IsNullOrEmpty(text) Then Return False
            If font Is Nothing Then Throw New ArgumentNullException("font")
            If rect.Width < 0 OrElse rect.Height < 0 Then Throw New ArgumentOutOfRangeException("rect")
            Return True
        End Function
        ''' <summary>Frees the resources consumed by this TextPrinter object.</summary>
        Friend Sub Dispose() Implements IDisposable.Dispose
            If Not disposed Then
                TextOutput.Dispose()
                disposed = True
            End If
        End Sub
    End Class

    ''' <summary> Holds the results of a text measurement. </summary>
    Friend Class TextExtents
        Implements IDisposable
#Region "Fields"
        Protected text_extents As RectangleF
        Protected glyph_extents As List(Of RectangleF) = New List(Of RectangleF)()
        Public Shared ReadOnly Empty As TextExtents = New TextExtents()
#End Region
        Friend Sub New()
        End Sub
#Region "Public Members"
        ''' <summary> Gets the bounding box of the measured text. </summary>
        Friend Property BoundingBox As RectangleF
            Get
                Return text_extents
            End Get
            Set(value As RectangleF)
                text_extents = value
            End Set
        End Property
        ''' <summary> Gets the extents of each glyph in the measured text. </summary>
        ''' <param name="i">The index of the glyph.</param>
        ''' <returns>The extents of the specified glyph.</returns>
        Default Friend Property Item(i As Integer) As RectangleF
            Get
                Return glyph_extents(i)
            End Get
            Set(value As RectangleF)
                glyph_extents(i) = value
            End Set
        End Property
        ''' <summary> Gets the extents of each glyph in the measured text. </summary>
        Friend ReadOnly Property GlyphExtents As IEnumerable(Of RectangleF)
            Get
                Return glyph_extents
            End Get
        End Property
        ''' <summary> Gets the number of the measured glyphs. </summary>
        Friend ReadOnly Property Count As Integer
            Get
                Return glyph_extents.Count
            End Get
        End Property
#End Region
#Region "Internal Members"
        Friend Sub Add(glyphExtent As RectangleF)
            glyph_extents.Add(glyphExtent)
        End Sub

        Friend Sub AddRange(glyphExtents As IEnumerable(Of RectangleF))
            glyph_extents.AddRange(glyphExtents)
        End Sub

        Friend Sub Clear()
            BoundingBox = RectangleF.Empty
            glyph_extents.Clear()
        End Sub

        Friend Function Clone() As TextExtents
            Dim extents As TextExtents = New TextExtents()
            extents.glyph_extents.AddRange(GlyphExtents)
            extents.BoundingBox = BoundingBox
            Return extents
        End Function
#End Region
        ''' <summary> Frees the resources consumed by this TextExtents instance. </summary>
        Friend Overridable Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class

    ''' <summary> Represents exceptions related to IGraphicsResources. </summary>
    Friend Class GraphicsResourceException
        Inherits Exception

        ''' <summary>Constructs a new GraphicsResourceException.</summary>
        Friend Sub New()
            MyBase.New()
        End Sub

        ''' <summary>Constructs a new string with the specified error message.</summary>
        Friend Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class

    Friend MustInherit Class Texture2D
        Implements IGraphicsResource, IEquatable(Of Texture2D)
#Region "Fields"
        Private contextField As IGraphicsContext
        Private idField As Integer
        Private widthField, heightField As Integer
        Private disposed As Boolean
        Private mag_filter As TextureMagFilter = TextureMagFilter.Linear
        Private min_filter As TextureMinFilter = TextureMinFilter.Linear
#End Region
        Friend Sub New(width As Integer, height As Integer)
            If width <= 0 Then Throw New ArgumentOutOfRangeException("width")
            If height <= 0 Then Throw New ArgumentOutOfRangeException("height")
            Me.Width = width
            Me.Height = height
        End Sub
#Region "Public Members"
        ''' <summary>Gets the width of the texture.</summary>
        Friend Property Width As Integer
            Get
                Return widthField
            End Get
            Private Set(value As Integer)
                widthField = value
            End Set
        End Property
        ''' <summary>Gets the height of the texture.</summary>
        Friend Property Height As Integer
            Get
                Return heightField
            End Get
            Private Set(value As Integer)
                heightField = value
            End Set
        End Property

        Friend Property MagnificationFilter As TextureMagFilter
            Get
                Return mag_filter
            End Get
            Set(value As TextureMagFilter)
                mag_filter = value
            End Set
        End Property

        Friend Property MinificationFilter As TextureMinFilter
            Get
                Return min_filter
            End Get
            Set(value As TextureMinFilter)
                min_filter = value
            End Set
        End Property

        Friend Sub Bind()
            Call GL.BindTexture(TextureTarget.Texture2D, TryCast(Me, IGraphicsResource).Id)
        End Sub

        Friend Sub WriteRegion(source As Rectangle, target As Rectangle, mipLevel As Integer, bitmap As Bitmap)
            If bitmap Is Nothing Then Throw New ArgumentNullException("data")
            Dim unit As GraphicsUnit = GraphicsUnit.Pixel
            If Not bitmap.GetBounds(unit).Contains(source) Then Throw New InvalidOperationException("The source Rectangle is larger than the Bitmap.")
            If mipLevel < 0 Then Throw New ArgumentOutOfRangeException("mipLevel")
            Bind()
            Dim data As BitmapData = Nothing
            GL.PushClientAttrib(ClientAttribMask.ClientPixelStoreBit)

            Try
                data = bitmap.LockBits(source, ImageLockMode.ReadOnly, Imaging.PixelFormat.Format32bppArgb)
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1)
                GL.PixelStore(PixelStoreParameter.UnpackRowLength, bitmap.Width)
                GL.TexSubImage2D(TextureTarget.Texture2D, mipLevel, target.Left, target.Top, target.Width, target.Height, OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0)
            Finally
                Call GL.PopClientAttrib()
                If data IsNot Nothing Then bitmap.UnlockBits(data)
            End Try
        End Sub

        Friend Function ReadRegion(rect As Rectangle, mipLevel As Integer) As TextureRegion2D
            If mipLevel < 0 Then Throw New ArgumentOutOfRangeException("miplevel")
            Dim region As TextureRegion2DType(Of Integer) = New TextureRegion2DType(Of Integer)(rect)
            GL.GetTexImage(TextureTarget.Texture2D, mipLevel, OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, region.Data)
            Return region
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If TypeOf obj Is Texture2D Then Return Equals(CType(obj, Texture2D))
            Return False
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return TryCast(Me, IGraphicsResource).Id
        End Function

        Public Overrides Function ToString() As String
            Return String.Format("Texture2D #{0} ({1}x{2}, {3})", TryCast(Me, IGraphicsResource).Id.ToString(), Width.ToString(), Height.ToString(), InternalFormat.ToString())
        End Function
#End Region
#Region "Protected Members"
        Protected MustOverride ReadOnly Property InternalFormat As PixelInternalFormat
#End Region
#Region "Private Members"
        Private Function CreateTexture(width As Integer, height As Integer) As Integer
            Dim id As Integer = GL.GenTexture()
            If id = 0 Then Throw New GraphicsResourceException(String.Format("Texture creation failed, (Error: {0})", GL.GetError()))
            SetDefaultTextureParameters(id)
            GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat, Me.Width, Me.Height, 0, OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero)
            Return id
        End Function

        Private Sub SetDefaultTextureParameters(id As Integer)
            ' Ensure the texture is allocated.
            GL.BindTexture(TextureTarget.Texture2D, id)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Linear)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Linear)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, All.ClampToEdge)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, All.ClampToEdge)
        End Sub
#End Region
#Region "IGraphicsResource Members"
        Private ReadOnly Property Context As IGraphicsContext Implements IGraphicsResource.Context
            Get
                Return contextField
            End Get
        End Property
        Private ReadOnly Property Id As Integer Implements IGraphicsResource.Id
            Get

                If idField = 0 Then
                    Call GraphicsContext.Assert()
                    contextField = GraphicsContext.CurrentContext
                    idField = CreateTexture(Width, Height)
                End If

                Return idField
            End Get
        End Property
#End Region
        Friend Overloads Function Equals(other As Texture2D) As Boolean Implements IEquatable(Of Texture2D).Equals
            Return TryCast(Me, IGraphicsResource).Id = TryCast(other, IGraphicsResource).Id
        End Function

        Friend Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Private Sub Dispose(manual As Boolean)
            If Not disposed Then

                If manual Then
                    GL.DeleteTexture(idField)
                Else
                    Debug.Print("[Warning] {0} leaked.", Me)
                End If

                disposed = True
            End If
        End Sub

        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub
    End Class

    Friend MustInherit Class TextureRegion2D
        Private rectangleField As Rectangle

        Public Property Rectangle As Rectangle
            Get
                Return rectangleField
            End Get
            Protected Set(value As Rectangle)
                rectangleField = value
            End Set
        End Property
    End Class

    ''' <summary> Holds part or the whole of a 2d OpenGL texture. </summary>
    Friend Class TextureRegion2DType(Of T As Structure)
        Inherits TextureRegion2D

        Private ReadOnly _Data As T(,)

        Friend Sub New(rect As Rectangle)
            _Data = New T(rect.Width - 1, rect.Height - 1) {}
            Rectangle = rect
        End Sub
        Default Friend Property Item(x As Integer, y As Integer) As T
            Get
                Return Me.Data(x, y)
            End Get
            Set(value As T)
                Me.Data(x, y) = value
            End Set
        End Property
        Friend ReadOnly Property Data As T(,)
            Get
                Return _Data
            End Get
        End Property
    End Class

    Friend Class RgbaTexture2D
        Inherits Texture2D

        Public Sub New(width As Integer, height As Integer)
            MyBase.New(width, height)
        End Sub

        Protected Overrides ReadOnly Property InternalFormat As PixelInternalFormat
            Get
                Return PixelInternalFormat.Rgba
            End Get
        End Property
    End Class

    Friend Class AlphaTexture2D
        Inherits Texture2D

        ''' <summary> Constructs a new Texture. </summary>
        Friend Sub New(width As Integer, height As Integer)
            MyBase.New(width, height)
        End Sub

        Protected Overrides ReadOnly Property InternalFormat As PixelInternalFormat
            Get
                Return PixelInternalFormat.Alpha
            End Get
        End Property
    End Class

    Friend Class TextureFont
        Implements IFont

        Friend font As Font
        Private loaded_glyphs As Dictionary(Of Char, RectangleF) = New Dictionary(Of Char, RectangleF)(64)
        Private bmp As Bitmap
        Private gfx As Drawing.Graphics
        ' TODO: We need to be able to use multiple font sheets.
        Private Shared textureField As Integer
        Private Shared pack As TexturePacker(Of GlyphPackable)
        Private Shared texture_width, texture_height As Integer
        Private Shared ReadOnly default_string_format As StringFormat = StringFormat.GenericTypographic ' Check the constructor, too, for additional flags.
        Private Shared ReadOnly load_glyph_string_format As StringFormat = StringFormat.GenericDefault
        Private Shared maximum_graphics_size As SizeF
        Private ReadOnly data As Integer() = New Integer(255) {}  ' Used to upload the glyph buffer to the OpenGL texture.
        Private ReadOnly upload_lock As Object = New Object()
        Private Shared ReadOnly newline_characters As Char() = New Char() {Convert.ToChar(10), Convert.ToChar(13)}
#Region "--- Constructor ---"
        ''' <summary>  Constructs a new TextureFont, using the specified System.Drawing.Font. </summary>
        ''' <param name="font">The System.Drawing.Font to use.</param>
        Friend Sub New(font As Font)
            If font Is Nothing Then Throw New ArgumentNullException("font", "Argument to TextureFont constructor cannot be null.")
            Me.font = font
            bmp = New Bitmap(font.Height * 2, font.Height * 2)
            gfx = Drawing.Graphics.FromImage(bmp)
            maximum_graphics_size = gfx.ClipBounds.Size


            ' Adjust font rendering mode. Small sizes look blurry without gridfitting, so turn
            ' that on. Increasing contrast also seems to help.
            If font.Size <= 18.0F Then
                'gfx.TextContrast = 11;
                gfx.TextRenderingHint = TextRenderingHint.AntiAliasGridFit
            Else
                gfx.TextRenderingHint = TextRenderingHint.AntiAlias
                'gfx.TextContrast = 0;
            End If

            default_string_format.FormatFlags = default_string_format.FormatFlags Or StringFormatFlags.MeasureTrailingSpaces
        End Sub
        ''' <summary> Constructs a new TextureFont, using the specified parameters. </summary>
        ''' <param name="family">The System.Drawing.FontFamily to use for the typeface.</param>
        ''' <param name="emSize">The em size to use for the typeface.</param>
        Friend Sub New(family As FontFamily, emSize As Single)
            Me.New(New Font(family, emSize))
        End Sub
        ''' <summary> Constructs a new TextureFont, using the specified parameters. </summary>
        ''' <param name="family">The System.Drawing.FontFamily to use for the typeface.</param>
        ''' <param name="emSize">The em size to use for the typeface.</param>
        ''' <param name="style">The style to use for the typeface.</param>
        Friend Sub New(family As FontFamily, emSize As Single, style As FontStyle)
            Me.New(New Font(family, emSize, style))
        End Sub
#End Region
#Region "--- Public Methods ---"
        ''' <summary> Prepares the specified glyphs for rendering. </summary>
        ''' <param name="glyphs">The glyphs to prepare for rendering.</param>
        Friend Sub LoadGlyphs(glyphs As String) Implements IFont.LoadGlyphs
            Dim rect As RectangleF = New RectangleF()

            For Each c As Char In glyphs
                If Char.IsWhiteSpace(c) Then Continue For

                Try
                    If Not loaded_glyphs.ContainsKey(c) Then LoadGlyph(c, rect)
                Catch e As Exception
                    Call Debug.Print(e.ToString())
                    Throw
                End Try
            Next
        End Sub
        ''' <summary> Prepares the specified glyph for rendering. </summary>
        ''' <param name="glyph">The glyph to prepare for rendering.</param>
        Friend Sub LoadGlyph(glyph As Char)
            Dim rect As RectangleF = New RectangleF()
            If Not loaded_glyphs.ContainsKey(glyph) Then LoadGlyph(glyph, rect)
        End Sub
        ''' <summary> Returns the characteristics of a loaded glyph. </summary>
        ''' <param name="glyph">The character corresponding to this glyph.</param>
        ''' <param name="width">The width of this glyph.</param>
        ''' <param name="height">The height of this glyph (line spacing).</param>
        ''' <param name="textureRectangle">The bounding box of the texture buffer of this glyph.</param>
        ''' <param name="texture">The handle to the texture that contains this glyph.</param>
        ''' <returns>True if the glyph has been loaded, false otherwise.</returns>
        ''' <seealso cref="LoadGlyphs"/>
        Friend Function GlyphData(glyph As Char, ByRef width As Single, ByRef height As Single, ByRef textureRectangle As RectangleF, ByRef texture As Integer) As Boolean
            If loaded_glyphs.TryGetValue(glyph, textureRectangle) Then
                width = textureRectangle.Width * texture_width
                height = textureRectangle.Height * texture_height
                texture = textureField
                Return True
            End If

            width = 0
            height = 0
            texture = 0
            Return False
        End Function
#End Region
        ''' <summary> Gets a float indicating the default line spacing of this font. </summary>
        Friend ReadOnly Property Height As Single Implements IFont.Height
            Get
                Return font.Height
            End Get
        End Property
        ''' <summary>
        ''' Gets a float indicating the default size of this font, in points.
        ''' </summary>
        Friend ReadOnly Property Width As Single
            Get
                Return font.SizeInPoints
            End Get
        End Property
        ''' <summary> Measures the width of the specified string. </summary>
        ''' <param name="str">The string to measure.</param>
        ''' <param name="width">The measured width.</param>
        ''' <param name="height">The measured height.</param>
        ''' <param name="accountForOverhangs">If true, adds space to account for glyph overhangs. Set to true if you wish to measure a complete string. 
        ''' Set to false if you wish to perform layout on adjacent strings.</param>
        Friend Sub MeasureString(str As String, ByRef width As Single, ByRef height As Single, accountForOverhangs As Boolean)
            Dim format As StringFormat = If(accountForOverhangs, StringFormat.GenericDefault, StringFormat.GenericTypographic)
            Dim size As SizeF = gfx.MeasureString(str, font, 16384, format)
            height = size.Height
            width = size.Width
        End Sub
        ''' <summary> Measures the width of the specified string. </summary>
        ''' <param name="str">The string to measure.</param>
        ''' <param name="width">The measured width.</param>
        ''' <param name="height">The measured height.</param>
        ''' <seealso cref="MeasureString"/>
        Friend Sub MeasureString(str As String, ByRef width As Single, ByRef height As Single) Implements IFont.MeasureString
            MeasureString(str, width, height, True)
        End Sub
        ''' <summary> Calculates size information for the specified text. </summary>
        ''' <param name="text">The string to measure.</param>
        ''' <returns>A RectangleF containing the bounding box for the specified text.</returns>
        Friend Function MeasureText(text As String) As RectangleF
            Return MeasureText(text, SizeF.Empty, default_string_format, Nothing)
        End Function
        ''' <summary> Calculates size information for the specified text. </summary>
        ''' <param name="text">The string to measure.</param>
        ''' <param name="bounds">A SizeF structure containing the maximum desired width and height of the text. Default is SizeF.Empty.</param>
        ''' <returns>A RectangleF containing the bounding box for the specified text.</returns>
        Friend Function MeasureText(text As String, bounds As SizeF) As RectangleF
            Return MeasureText(text, bounds, default_string_format, Nothing)
        End Function
        ''' <summary> Calculates size information for the specified text. </summary>
        ''' <param name="text">The string to measure.</param>
        ''' <param name="bounds">A SizeF structure containing the maximum desired width and height of the text. Pass SizeF.Empty to disable wrapping calculations. 
        ''' A width or height of 0 disables the relevant calculation.</param>
        ''' <param name="format">A StringFormat object which specifies the measurement format of the string. Pass null to use the default StringFormat (StringFormat.GenericTypographic).</param>
        ''' <returns>A RectangleF containing the bounding box for the specified text.</returns>
        Friend Function MeasureText(text As String, bounds As SizeF, format As StringFormat) As RectangleF
            Return MeasureText(text, bounds, format, Nothing)
        End Function

        Private ReadOnly regions As IntPtr() = New IntPtr(MaxMeasurableCharacterRanges - 1) {}
        Private ReadOnly characterRanges As CharacterRange() = New CharacterRange(MaxMeasurableCharacterRanges - 1) {}

        ''' <summary> Calculates size information for the specified text. </summary>
        ''' <param name="text">The string to measure.</param>
        ''' <param name="bounds">A SizeF structure containing the maximum desired width and height of the text. Pass SizeF.Empty to disable wrapping calculations. 
        ''' A width or height of 0 disables the relevant calculation.</param>
        ''' <param name="format">A StringFormat object which specifies the measurement format of the string. Pass null to use the default StringFormat (StringFormat.GenericDefault).</param>
        ''' <param name="ranges">Fills the specified IList of RectangleF structures with position information for individual characters. If this argument is null, these calculations are skipped.</param>
        ''' <returns>A RectangleF containing the bounding box for the specified text.</returns>
        Friend Function MeasureText(text As String, bounds As SizeF, format As StringFormat, ranges As List(Of RectangleF)) As RectangleF
            If String.IsNullOrEmpty(text) Then Return RectangleF.Empty
            If bounds = SizeF.Empty Then bounds = maximum_graphics_size
            If format Is Nothing Then format = default_string_format

            ' TODO: What should we do in this case?
            If ranges Is Nothing Then ranges = New List(Of RectangleF)()
            ranges.Clear()
            Dim origin As PointF = PointF.Empty
            Dim size As SizeF = SizeF.Empty
            Dim native_graphics As IntPtr = GetNativeGraphics(gfx)
            Dim native_font As IntPtr = GetNativeFont(font)
            Dim native_string_format As IntPtr = GetNativeStringFormat(format)
            Dim layoutRect As RectangleF = New RectangleF(PointF.Empty, bounds)
            Dim height As Integer = 0


            ' It seems that the mere presence of \n and \r characters
            ' is enough for Mono to botch the layout (even if these
            ' characters are not processed.) We'll need to find a
            ' different way to perform layout on Mono, probably
            ' through Pango.
            ' Todo: This workaround  allocates memory.
            'if (Configuration.RunningOnMono)
            If True Then
                Dim lines As String() = text.Replace(CStr(Microsoft.VisualBasic.Constants.vbCr), CStr(String.Empty)).Split(newline_characters)

                For Each s As String In lines
                    ranges.AddRange(GetCharExtents(s, height, 0, s.Length, layoutRect, native_graphics, native_font, native_string_format))
                    height += font.Height
                Next
            End If

            Return New RectangleF(ranges(0).X, ranges(0).Y, ranges(ranges.Count - 1).Right, ranges(ranges.Count - 1).Bottom)
        End Function

        ''' <summary> Calculates the optimal size for the font texture and TexturePacker, and creates both. </summary>
        Private Sub PrepareTexturePacker()
            ' Calculate the size of the texture packer. We want a power-of-two size
            ' that is less than 1024 (supported in Geforce256-era cards), but large
            ' enough to hold at least 256 (16*16) font glyphs.
            ' TODO: Find the actual card limits, maybe?
            Dim size As Integer = CInt(Math.Floor(font.Size * 16))
            size = CInt(Math.Pow(2.0, Math.Ceiling(Math.Log(size, 2.0))))
            If size > 1024 Then size = 1024
            texture_width = size
            texture_height = size
            pack = New TexturePacker(Of GlyphPackable)(texture_width, texture_height)
            GL.GenTextures(1, textureField)
            GL.BindTexture(TextureTarget.Texture2D, textureField)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, All.Linear)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, All.Linear)

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, All.ClampToEdge)
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, All.ClampToEdge)

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Alpha, texture_width, texture_height, 0, OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero)
        End Sub
        ' Adds the specified caharacter to the texture packer.
        Private Sub LoadGlyph(c As Char, ByRef rectangle As RectangleF)
            If pack Is Nothing Then PrepareTexturePacker()
            Dim glyph_rect As RectangleF = MeasureText(c.ToString(), SizeF.Empty, load_glyph_string_format)
            Dim glyph_size As SizeF = New SizeF(glyph_rect.Right, glyph_rect.Bottom)  ' We need to do this, since the origin might not be (0, 0)
            Dim g As GlyphPackable = New GlyphPackable(c, font, glyph_size)
            Dim rect As Rectangle

            Try
                pack.Add(g, rect)
            Catch expt As InvalidOperationException
                ' TODO: The TexturePacker is full, create a new font sheet.
                Trace.WriteLine(expt)
                Throw
            End Try

            GL.BindTexture(TextureTarget.Texture2D, textureField)
            gfx.Clear(Color.Transparent)
            gfx.DrawString(g.Character.ToString(), g.Font, Brushes.White, 0.0F, 0.0F, default_string_format)
            Dim bitmap_data As BitmapData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, Imaging.PixelFormat.Format32bppArgb)
            GL.PushClientAttrib(ClientAttribMask.ClientPixelStoreBit)

            Try
                GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1.0F)
                GL.PixelStore(PixelStoreParameter.UnpackRowLength, bmp.Width)
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, rect.Left, rect.Top, rect.Width, rect.Height, OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, bitmap_data.Scan0)
            Finally
                Call GL.PopClientAttrib()
            End Try

            bmp.UnlockBits(bitmap_data)
            rectangle = RectangleF.FromLTRB(rect.Left / CSng(texture_width), rect.Top / CSng(texture_height), rect.Right / CSng(texture_width), rect.Bottom / CSng(texture_height))
            loaded_glyphs.Add(g.Character, rectangle)
        End Sub

        ' Gets the bounds of each character in a line of text.
        ' The line is processed in blocks of 32 characters (GdiPlus.MaxMeasurableCharacterRanges).
        Private Iterator Function GetCharExtents(text As String, height As Integer, line_start As Integer, line_length As Integer,
                                                 layoutRect As RectangleF, native_graphics As IntPtr, native_font As IntPtr, native_string_format As IntPtr) As IEnumerable(Of RectangleF)
            Dim rect As RectangleF = New RectangleF()
            Dim line_end As Integer = line_start + line_length

            While line_start < line_end
                Dim num_characters As Integer = If(line_end - line_start > MaxMeasurableCharacterRanges, MaxMeasurableCharacterRanges, line_end - line_start)
                Dim status As Integer = 0

                For i As Integer = 0 To num_characters - 1
                    characterRanges(i) = New CharacterRange(line_start + i, 1)
                    Dim region As IntPtr
                    status = CreateRegion(region)
                    regions(i) = region
                    Debug.Assert(status = 0, String.Format("GDI+ error: {0}", status))
                Next

                status = SetStringFormatMeasurableCharacterRanges(native_string_format, num_characters, characterRanges)
                Debug.Assert(status = 0, String.Format("GDI+ error: {0}", status))
                status = MeasureCharacterRanges(native_graphics, text, text.Length, native_font, layoutRect, native_string_format, num_characters, regions)
                Debug.Assert(status = 0, String.Format("GDI+ error: {0}", status))

                For i As Integer = 0 To num_characters - 1
                    GetRegionBounds(regions(i), native_graphics, rect)
                    Debug.Assert(status = 0, String.Format("GDI+ error: {0}", status))
                    DeleteRegion(regions(i))
                    Debug.Assert(status = 0, String.Format("GDI+ error: {0}", status))
                    rect.Y += height
                    Yield rect
                Next

                line_start += num_characters
            End While
        End Function

        ''' <summary> Gets the handle to the texture were this font resides. </summary>
        Friend ReadOnly Property Texture As Integer
            Get
                Return textureField
            End Get
        End Property
#Region "--- IDisposable Members ---"
        Private disposed As Boolean
        ''' <summary>Releases all resources used by this OpenTK.Graphics.TextureFont.</summary>
        Friend Sub Dispose() Implements IDisposable.Dispose
            GC.SuppressFinalize(Me)
            Dispose(True)
        End Sub

        Private Sub Dispose(manual As Boolean)
            If Not disposed Then
                pack = Nothing

                If manual Then
                    GL.DeleteTextures(1, textureField)
                    font.Dispose()
                    gfx.Dispose()
                End If

                disposed = True
            End If
        End Sub
        ''' <summary> Finalizes this TextureFont. </summary>
        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub
#End Region
    End Class

    ''' <summary> Represents a handle to cached text. </summary>
    Friend Class TextHandle
        Implements IDisposable

        Friend Text As String
        Friend GdiPFont As Font

        ''' <summary> Constructs a new TextHandle, </summary>
        ''' <param name="handle"></param>
        Friend Sub New(handle As Integer)
            Me.Handle = handle
        End Sub

        Friend Sub New(text As String, font As Font)
            Me.Text = text
            GdiPFont = font
        End Sub

        Private handleField As Integer
        Protected fontField As TextureFont
        Protected disposed As Boolean
        ''' <summary> Gets the handle of the cached text run. Call the OpenTK.Graphics.ITextPrinter.Draw() method
        ''' to draw the text represented by this TextHandle. </summary>
        Friend Property Handle As Integer
            Get
                Return handleField
            End Get
            Set(value As Integer)
                handleField = value
            End Set
        End Property

        ''' <summary>Gets the TextureFont used for this text run.</summary>
        Friend Property Font As TextureFont
            Get
                Return fontField
            End Get
            Set(value As TextureFont)
                fontField = value
            End Set
        End Property
        ''' <summary> Returns a System.String that represents the current TextHandle. </summary>
        ''' <returns>a System.String that descibes the current TextHandle.</returns>
        Public Overrides Function ToString() As String
            Return String.Format("TextHandle: {0}", Handle)
        End Function
#Region "--- IDisposable Members ---"
        ''' <summary> Frees the resource consumed by the TextHandle. </summary>
        Friend Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overridable Sub Dispose(manual As Boolean)
        End Sub

        Protected Overrides Sub Finalize()
            Dispose(False)
        End Sub
#End Region
    End Class

    '''<summary> Provides methods to access internal GdiPlus fields. This is necessary for
    ''' managed -> native GdiPlus interoperability.
    ''' Note that the fields are named differently between .Net and Mono.
    ''' GdiPlus is considered deprecated by Microsoft - it is highly unlikely that
    ''' future framework upgrades will break this code, but it is something to
    ''' keep in mind.</summary>
    Friend Module GdiPlus
        Private internals As IGdiPlusInternals
        Const gdi_plus_library As String = "gdiplus.dll"
        Sub New()
            internals = New WinGdiPlusInternals()
        End Sub

#Region "--- Public Methods ---"
        Friend Function GetNativeGraphics(graphics As Drawing.Graphics) As IntPtr
            Return internals.GetNativeGraphics(graphics)
        End Function

        Friend Function GetNativeFont(font As Font) As IntPtr
            Return internals.GetNativeFont(font)
        End Function

        Friend Function GetNativeStringFormat(format As StringFormat) As IntPtr
            Return internals.GetNativeStringFormat(format)
        End Function

        Friend ReadOnly Property MaxMeasurableCharacterRanges As Integer
            Get
                Return 32    ' This is a GDI+ limitation. TODO: Can we query this somehow? 
            End Get
        End Property

        <DllImport(gdi_plus_library, CharSet:=CharSet.Unicode, SetLastError:=True, ExactSpelling:=True, EntryPoint:="GdipSetStringFormatMeasurableCharacterRanges")>
        Public Function SetStringFormatMeasurableCharacterRanges(format As HandleRef, rangeCount As Integer,
        <[In], Out> range As CharacterRange()) As Integer
        End Function

        <DllImport(gdi_plus_library, CharSet:=CharSet.Unicode, SetLastError:=True, ExactSpelling:=True, EntryPoint:="GdipSetStringFormatMeasurableCharacterRanges")>
        Public Function SetStringFormatMeasurableCharacterRanges(format As IntPtr, rangeCount As Integer,
        <[In], Out> range As CharacterRange()) As Integer
        End Function

        <DllImport(gdi_plus_library, CharSet:=CharSet.Unicode, SetLastError:=True, ExactSpelling:=True, EntryPoint:="GdipGetStringFormatMeasurableCharacterRangeCount")>
        Public Function GetStringFormatMeasurableCharacterRangeCount(format As HandleRef, ByRef count As Integer) As Integer
        End Function

        <DllImport(gdi_plus_library, CharSet:=CharSet.Unicode, SetLastError:=True, ExactSpelling:=True, EntryPoint:="GdipMeasureCharacterRanges")>
        Public Function MeasureCharacterRanges(graphics As HandleRef, textString As String, length As Integer, font As HandleRef, ByRef layoutRect As RectangleF, stringFormat As HandleRef, characterCount As Integer,
        <[In], Out> region As IntPtr()) As Integer
        End Function
        <DllImport(gdi_plus_library, CharSet:=CharSet.Unicode, SetLastError:=True, ExactSpelling:=True, EntryPoint:="GdipMeasureCharacterRanges")>
        Public Function MeasureCharacterRanges(graphics As IntPtr, textString As String, length As Integer, font As IntPtr, ByRef layoutRect As RectangleF, stringFormat As IntPtr, characterCount As Integer,
        <[In], Out> region As IntPtr()) As Integer
        End Function

        <DllImport(gdi_plus_library, CharSet:=CharSet.Unicode, SetLastError:=True, ExactSpelling:=True, EntryPoint:="GdipGetRegionBounds")>
        Public Function GetRegionBounds(region As IntPtr, graphics As HandleRef, ByRef gprectf As RectangleF) As Integer
        End Function

        <DllImport(gdi_plus_library, CharSet:=CharSet.Unicode, SetLastError:=True, ExactSpelling:=True, EntryPoint:="GdipGetRegionBounds")>
        Public Function GetRegionBounds(region As IntPtr, graphics As IntPtr, ByRef gprectf As RectangleF) As Integer
        End Function

        <DllImport(gdi_plus_library, EntryPoint:="GdipCreateRegion", CharSet:=CharSet.Unicode, SetLastError:=True, ExactSpelling:=True)>
        Public Function CreateRegion(ByRef region As IntPtr) As Integer
        End Function

        <DllImport(gdi_plus_library, EntryPoint:="GdipDeleteRegion", CharSet:=CharSet.Unicode, SetLastError:=True, ExactSpelling:=True)>
        Public Function DeleteRegion(region As IntPtr) As Integer
        End Function
#End Region
    End Module

    Friend Class WinGdiPlusInternals
        Implements IGdiPlusInternals

        Private Shared ReadOnly native_graphics_property, native_font_property As PropertyInfo
        Private Shared ReadOnly native_string_format_field As FieldInfo
        Shared Sub New()
            native_graphics_property = GetType(Graphics).GetProperty("NativeGraphics", BindingFlags.Instance Or BindingFlags.NonPublic)

            native_font_property = GetType(Font).GetProperty("NativeFont", BindingFlags.Instance Or BindingFlags.NonPublic)

            native_string_format_field = GetType(StringFormat).GetField("nativeFormat", BindingFlags.Instance Or BindingFlags.NonPublic)
        End Sub
#Region "--- IGdiPlusInternals Members ---"
        Friend Function GetNativeGraphics(graphics As Graphics) As IntPtr Implements IGdiPlusInternals.GetNativeGraphics
            Return CType(native_graphics_property.GetValue(graphics, Nothing), IntPtr)
        End Function
        Friend Function GetNativeFont(font As Font) As IntPtr Implements IGdiPlusInternals.GetNativeFont
            Return CType(native_font_property.GetValue(font, Nothing), IntPtr)
        End Function

        Friend Function GetNativeStringFormat(format As StringFormat) As IntPtr Implements IGdiPlusInternals.GetNativeStringFormat
            Return CType(native_string_format_field.GetValue(format), IntPtr)
        End Function
#End Region
    End Class

    '''<summary> Uniquely identifies a block of text. This structure can be used to identify text blocks for caching.</summary>
    Friend MustInherit Class GL1TextOutputProvider
        Implements ITextOutputProvider

        ' Triangle lists, sorted by texture.
        Private active_lists As Dictionary(Of Texture2D, List(Of Vector2)) = New Dictionary(Of Texture2D, List(Of Vector2))()
        Private inactive_lists As Queue(Of List(Of Vector2)) = New Queue(Of List(Of Vector2))()

        Friend Structure Viewport
            Public X, Y, Width, Height As Integer
        End Structure

        ' Used to save the current state in Begin() and restore it in End()
        Private projection_stack As Stack(Of Matrix4) = New Stack(Of Matrix4)()
        Private modelview_stack As Stack(Of Matrix4) = New Stack(Of Matrix4)()
        Private texture_stack As Stack(Of Matrix4) = New Stack(Of Matrix4)()
        Private viewport_stack As Stack(Of Viewport) = New Stack(Of Viewport)()

        ' Used as temporary storage when saving / restoring the current state.
        Private viewportField As Viewport = New Viewport()
        Private matrix As Matrix4 = New Matrix4()

        ' TextBlock - display list cache.
        ' Todo: we need a cache eviction strategy.
        Const block_cache_capacity As Integer = 32
        Private ReadOnly block_cache As Dictionary(Of Integer, Integer) = New Dictionary(Of Integer, Integer)(block_cache_capacity)
        Private disposed As Boolean

        Friend Sub New()
            inactive_lists.Enqueue(New List(Of Vector2)())
        End Sub

#Region "ITextOutputProvider Members"
        Friend Sub Print(ByRef block As TextBlock, color As Color, rasterizer As IGlyphRasterizer) Implements ITextOutputProvider.Print
            GL.PushAttrib(AttribMask.CurrentBit Or AttribMask.TextureBit Or AttribMask.EnableBit Or AttribMask.ColorBufferBit Or AttribMask.DepthBufferBit)
            GL.Enable(EnableCap.Texture2D)
            GL.Enable(EnableCap.Blend)
            SetBlendFunction()
            GL.Disable(EnableCap.DepthTest)
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, All.Modulate)
            Call GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvColor, New Color4(0, 0, 0, 0))
            GL.Disable(EnableCap.TextureGenQ)
            GL.Disable(EnableCap.TextureGenR)
            GL.Disable(EnableCap.TextureGenS)
            GL.Disable(EnableCap.TextureGenT)
            Dim position As RectangleF
            SetColor(color)
            Dim block_hash As Integer = block.GetHashCode()

            If block_cache.ContainsKey(block_hash) Then
                GL.CallList(block_cache(block_hash))
            Else

                Using extents As TextExtents = rasterizer.MeasureText(block)
                    ' Build layout
                    Dim current As Integer = 0

                    For Each glyph As GlyphEQuatable In block

                        ' Do not render whitespace characters or characters outside the clip rectangle.
                        If glyph.IsWhiteSpace OrElse extents(current).Width = 0 OrElse extents(current).Height = 0 Then
                            current += 1
                            Continue For
                        ElseIf Not Cache.Contains(glyph) Then
                            Cache.Add(glyph, rasterizer, TextQuality)
                        End If

                        Dim info As CachedGlyphInfo = Cache(glyph)
                        position = extents(Math.Min(Threading.Interlocked.Increment(current), current - 1))

                        ' Use the real glyph width instead of the measured one (we want to achieve pixel perfect output).
                        position.Size = info.Rectangle.Size

                        If Not active_lists.ContainsKey(info.Texture) Then

                            If inactive_lists.Count > 0 Then
                                Dim list As List(Of Vector2) = inactive_lists.Dequeue()
                                list.Clear()
                                active_lists.Add(info.Texture, list)
                            Else
                                active_lists.Add(info.Texture, New List(Of Vector2)())
                            End If
                        End If

                        If True Then
                            ' Interleaved array: Vertex, TexCoord, Vertex, ...
                            Dim current_list As List(Of Vector2) = active_lists(info.Texture)
                            current_list.Add(New Vector2(info.RectangleNormalized.Left, info.RectangleNormalized.Top))
                            current_list.Add(New Vector2(position.Left, position.Top))
                            current_list.Add(New Vector2(info.RectangleNormalized.Left, info.RectangleNormalized.Bottom))
                            current_list.Add(New Vector2(position.Left, position.Bottom))
                            current_list.Add(New Vector2(info.RectangleNormalized.Right, info.RectangleNormalized.Bottom))
                            current_list.Add(New Vector2(position.Right, position.Bottom))
                            current_list.Add(New Vector2(info.RectangleNormalized.Right, info.RectangleNormalized.Bottom))
                            current_list.Add(New Vector2(position.Right, position.Bottom))
                            current_list.Add(New Vector2(info.RectangleNormalized.Right, info.RectangleNormalized.Top))
                            current_list.Add(New Vector2(position.Right, position.Top))
                            current_list.Add(New Vector2(info.RectangleNormalized.Left, info.RectangleNormalized.Top))
                            current_list.Add(New Vector2(position.Left, position.Top))
                        End If
                    Next
                End Using


                ' Render
                Dim display_list As Integer = 0

                If (block.Options And TextPrinterOptions.NoCache) = 0 Then
                    display_list = GL.GenLists(1)
                    ' Mesa Indirect gerates an InvalidOperation error right after
                    ' GL.EndList() when using ListMode.CompileAndExecute.
                    ' Using ListMode.Compile as a workaround.
                    GL.NewList(display_list, ListMode.Compile)
                End If

                For Each key As Texture2D In active_lists.Keys
                    Dim list As List(Of Vector2) = active_lists(key)
                    key.Bind()

                    'GL.Begin(BeginMode.Triangles);
                    GL.Begin(PrimitiveType.Triangles)

                    For i As Integer = 0 To list.Count - 1 Step 2
                        GL.TexCoord2(list(i))
                        GL.Vertex2(list(i + 1))
                    Next

                    Call GL.End()
                Next

                If (block.Options And TextPrinterOptions.NoCache) = 0 Then
                    Call GL.EndList()
                    block_cache.Add(block_hash, display_list)
                    GL.CallList(display_list)
                End If


                ' Clean layout
                For Each list As List(Of Vector2) In active_lists.Values
                    'list.Clear();
                    inactive_lists.Enqueue(list)
                Next

                active_lists.Clear()
            End If

            Call GL.PopAttrib()
        End Sub

        Friend Sub Clear() Implements ITextOutputProvider.Clear
            Cache.Clear()

            For Each display_list As Integer In block_cache.Keys
                GL.DeleteLists(display_list, 1)
            Next

            block_cache.Clear()
        End Sub

        Friend Sub Begin() Implements ITextOutputProvider.Begin
            If disposed Then Throw New ObjectDisposedException([GetType]().ToString())
            Call GraphicsContext.Assert()

            ' Save the state of everything we are going to modify:
            ' the current matrix mode, viewport state and the projection, modelview and texture matrices.
            ' All these will be restored in the TextPrinter.End() method.
            Dim current_matrix As Integer
            GL.GetInteger(GetPName.MatrixMode, current_matrix)
            GL.GetInteger(GetPName.Viewport, viewportField.X)
            viewport_stack.Push(viewportField)
            GL.GetFloat(GetPName.ProjectionMatrix, matrix.Row0.X)
            projection_stack.Push(matrix)
            GL.GetFloat(GetPName.ModelviewMatrix, matrix.Row0.X)
            modelview_stack.Push(matrix)
            GL.GetFloat(GetPName.TextureMatrix, matrix.Row0.X)
            texture_stack.Push(matrix)

            ' Prepare to draw text. We want pixel perfect precision, so we setup a 2D mode,
            ' with size equal to the window (in pixels). 
            ' While we could also render text in 3D mode, it would be very hard to get
            ' pixel-perfect precision.
            GL.MatrixMode(MatrixMode.Projection)
            Call GL.LoadIdentity()
            GL.Ortho(viewportField.X, viewportField.Width, viewportField.Height, viewportField.Y, -1.0, 1.0)
            GL.MatrixMode(MatrixMode.Modelview)
            Call GL.LoadIdentity()
            GL.MatrixMode(MatrixMode.Texture)
            Call GL.LoadIdentity()
            GL.MatrixMode(CType(current_matrix, MatrixMode))
        End Sub

        Friend Sub [End]() Implements ITextOutputProvider.End
            If disposed Then Throw New ObjectDisposedException([GetType]().ToString())
            Call GraphicsContext.Assert()
            Dim current_matrix As Integer
            GL.GetInteger(GetPName.MatrixMode, current_matrix)
            viewportField = viewport_stack.Pop()
            GL.Viewport(viewportField.X, viewportField.Y, viewportField.Width, viewportField.Height)
            GL.MatrixMode(MatrixMode.Texture)
            matrix = texture_stack.Pop()
            GL.LoadMatrix(matrix)
            GL.MatrixMode(MatrixMode.Modelview)
            matrix = modelview_stack.Pop()
            GL.LoadMatrix(matrix)
            GL.MatrixMode(MatrixMode.Projection)
            matrix = projection_stack.Pop()
            GL.LoadMatrix(matrix)
            GL.MatrixMode(CType(current_matrix, MatrixMode))
        End Sub
#End Region
        Protected MustOverride Sub SetBlendFunction()
        Protected MustOverride Sub SetColor(color As Color)
        Protected MustOverride ReadOnly Property TextQuality As TextQuality
        Protected MustOverride ReadOnly Property Cache As GlyphCache

        Friend Shared Function Create(quality As TextQuality) As GL1TextOutputProvider
            If quality = TextQuality.Low OrElse quality = TextQuality.Medium Then
                Return New GL11TextOutputProvider(quality)
            Else
                Return New GL12TextOutputProvider(quality)
            End If
        End Function

        Friend Sub Dispose() Implements IDisposable.Dispose
            If Not disposed Then
                Cache.Dispose()
                disposed = True
            End If
        End Sub
    End Class

    Friend NotInheritable Class GL12TextOutputProvider
        Inherits GL1TextOutputProvider

        Private ReadOnly quality As TextQuality
        Private ReadOnly cacheField As GlyphCache

        Friend Sub New(quality As TextQuality)
            Me.quality = quality
            cacheField = New GlyphCacheType(Of RgbaTexture2D)()
        End Sub

        Protected Overrides Sub SetBlendFunction()
            'GL.BlendFunc(BlendingFactorSrc.ConstantColorExt, BlendingFactorDest.OneMinusSrcColor);
            GL.BlendFunc(BlendingFactor.ConstantColor, BlendingFactor.OneMinusSrcColor)
        End Sub

        Protected Overrides Sub SetColor(color As Color)
            GL.Color3(Color.White)
            GL.BlendColor(color)
        End Sub

        Protected Overrides ReadOnly Property TextQuality As TextQuality
            Get
                Return quality
            End Get
        End Property

        Protected Overrides ReadOnly Property Cache As GlyphCache
            Get
                Return cacheField
            End Get
        End Property
    End Class

    Friend NotInheritable Class GL11TextOutputProvider
        Inherits GL1TextOutputProvider

        Private ReadOnly quality As TextQuality
        Private cacheField As GlyphCache

        Friend Sub New(quality As TextQuality)
            If quality = TextQuality.High OrElse quality = TextQuality.Default Then
                Me.quality = TextQuality.Medium
            Else
                Me.quality = quality
            End If
        End Sub

        Protected Overrides Sub SetBlendFunction()
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)  ' For grayscale
        End Sub

        Protected Overrides Sub SetColor(color As Color)
            GL.Color3(color)
        End Sub

        Protected Overrides ReadOnly Property TextQuality As TextQuality
            Get
                Return quality
            End Get
        End Property

        Protected Overrides ReadOnly Property Cache As GlyphCache
            Get

                If cacheField Is Nothing Then

                    If GL.GetString(StringName.Renderer).Contains("ProSavage/Twister") Then
                        cacheField = New GlyphCacheType(Of RgbaTexture2D)()
                    Else
                        cacheField = New GlyphCacheType(Of AlphaTexture2D)()
                    End If
                End If

                Return cacheField
            End Get
        End Property
    End Class

    Friend MustInherit Class GlyphCache
        Implements IGlyphCache

        Friend MustOverride Sub Add(glyph As GlyphEQuatable, rasterizer As IGlyphRasterizer, quality As TextQuality) Implements IGlyphCache.Add
        Friend MustOverride Function Contains(glyph As GlyphEQuatable) As Boolean Implements IGlyphCache.Contains
        Default Friend MustOverride ReadOnly Property Item(glyph As GlyphEQuatable) As CachedGlyphInfo Implements IGlyphCache.Item
        Friend MustOverride Sub Clear() Implements IGlyphCache.Clear
        Friend MustOverride Sub Dispose() Implements IDisposable.Dispose
    End Class

    Friend NotInheritable Class GlyphCacheType(Of T As Texture2D)
        Inherits GlyphCache

        Private sheets As List(Of GlyphSheet(Of T)) = New List(Of GlyphSheet(Of T))()
        Private cached_glyphs As Dictionary(Of GlyphEQuatable, CachedGlyphInfo) = New Dictionary(Of GlyphEQuatable, CachedGlyphInfo)()
        Private disposed As Boolean
        Const SheetWidth As Integer = 512, SheetHeight As Integer = 512

        Friend Sub New()
            sheets.Add(New GlyphSheet(Of T)(SheetWidth, SheetHeight))
        End Sub

#Region "IGlyphCache Members"
        Friend Overrides Sub Add(glyph As GlyphEQuatable, rasterizer As IGlyphRasterizer, quality As TextQuality)
            If rasterizer Is Nothing Then Throw New ArgumentNullException("rasterizer")
            Dim inserted As Boolean = False

            Using bmp As Bitmap = rasterizer.Rasterize(glyph, quality)
                Dim rect As Rectangle = New Rectangle(0, 0, bmp.Width, bmp.Height)

                For Each sheet As GlyphSheet(Of T) In sheets
                    inserted = InsertGlyph(glyph, bmp, rect, sheet)
                    If inserted Then Exit For
                Next

                If Not inserted Then
                    Dim sheet As GlyphSheet(Of T) = New GlyphSheet(Of T)(SheetWidth, SheetHeight)
                    sheets.Add(sheet)
                    InsertGlyph(glyph, bmp, rect, sheet)
                End If
            End Using
        End Sub

        Friend Overrides Function Contains(glyph As GlyphEQuatable) As Boolean
            Return cached_glyphs.ContainsKey(glyph)
        End Function

        Default Friend Overrides ReadOnly Property Item(glyph As GlyphEQuatable) As CachedGlyphInfo
            Get
                Return cached_glyphs(glyph)
            End Get
        End Property

        Friend Overrides Sub Clear()
            For i As Integer = 0 To sheets.Count - 1
                sheets(i).Dispose()
            Next

            sheets.Clear()
        End Sub
#End Region
        ' Asks the packer for an empty space and writes the glyph there.
        Private Function InsertGlyph(glyph As GlyphEQuatable, bmp As Bitmap, source As Rectangle, sheet As GlyphSheet(Of T)) As Boolean
            Dim target As Rectangle = New Rectangle()
            If Not sheet.Packer.TryAdd(source, target) Then Return False
            sheet.Texture.WriteRegion(source, target, 0, bmp)
            cached_glyphs.Add(glyph, New CachedGlyphInfo(sheet.Texture, target))
            Return True
        End Function

        Friend Overrides Sub Dispose()
            If Not disposed Then
                Clear()
                disposed = True
            End If
        End Sub
    End Class

    Friend Class GlyphPacker
        Private root As Node
#Region "--- Public Methods ---"
        Friend Sub New(width As Integer, height As Integer)
            If width <= 0 Then Throw New ArgumentOutOfRangeException("width", width, "Must be greater than zero.")
            If height <= 0 Then Throw New ArgumentOutOfRangeException("height", height, "Must be greater than zero.")
            root = New Node With {.Rectangle = New Rectangle(0, 0, width, width)}
        End Sub
        ''' <summary> Adds boundingBox to the GlyphPacker. </summary>
        ''' <param name="boundingBox">The bounding box of the item to pack.</param>
        ''' <param name="packedRectangle">The System.Drawing.Rectangle that contains the position of the packed item.</param>
        ''' <returns>True, if the item was successfully packed; false if the item is too big for this packer..</returns>
        ''' <exception cref="InvalidOperationException">Occurs if the item is larger than the available TexturePacker area</exception>
        ''' <exception cref="TexturePackerFullException">Occurs if the item cannot fit in the remaining packer space.</exception>
        Friend Function TryAdd(boundingBox As Rectangle, ByRef packedRectangle As Rectangle) As Boolean
            If Not root.Rectangle.Contains(boundingBox) Then
                packedRectangle = New Rectangle()
                Return False
            End If
            ' Increase size so that the glyphs do not touch each other (to avoid rendering artifacts).
            boundingBox.Width += 2
            boundingBox.Height += 2
            Dim node As Node = root.Insert(boundingBox)
            ' Tree is full and insertion failed:
            If node Is Nothing Then
                packedRectangle = New Rectangle()
                Return False
            End If

            packedRectangle = New Rectangle(node.Rectangle.X, node.Rectangle.Y, node.Rectangle.Width - 2, node.Rectangle.Height - 2)
            Return True
        End Function
        ''' <summary> Adds boundingBox to the GlyphPacker. </summary>
        ''' <param name="boundingBox">The bounding box of the item to pack.</param>
        ''' <param name="packedRectangle">The System.Drawing.RectangleF that contains the position of the packed item.</param>
        ''' <returns>True, if the item was successfully packed; false if the item is too big for this packer..</returns>
        ''' <exception cref="InvalidOperationException">Occurs if the item is larger than the available TexturePacker area</exception>
        ''' <exception cref="TexturePackerFullException">Occurs if the item cannot fit in the remaining packer space.</exception>
        Friend Function TryAdd(boundingBox As RectangleF, ByRef packedRectangle As RectangleF) As Boolean
            Dim bbox As Rectangle = New Rectangle(CInt(Math.Floor(boundingBox.X)), CInt(Math.Floor(boundingBox.Y)),
                                                  CInt(Math.Floor(boundingBox.Width + 0.5F)), CInt(Math.Floor(boundingBox.Height + 0.5F)))
            Return TryAdd(bbox, packedRectangle)
        End Function
        ''' <summary> Adds boundingBox to the GlyphPacker. </summary>
        ''' <param name="boundingBox">The bounding box of the item to pack.</param>
        ''' <returns>A System.Drawing.Rectangle containing the coordinates of the packed item.</returns>
        ''' <exception cref="InvalidOperationException">Occurs if the item is larger than the available TexturePacker area</exception>
        ''' <exception cref="TexturePackerFullException">Occurs if the item cannot fit in the remaining packer space.</exception>
        Friend Function Add(boundingBox As Rectangle) As Rectangle
            If Not TryAdd(boundingBox, boundingBox) Then Throw New TexturePackerFullException()
            Return boundingBox
        End Function
        ''' <summary> Rounds boundingBox to the largest integer and adds the resulting Rectangle to the GlyphPacker. </summary>
        ''' <param name="boundingBox">The bounding box of the item to pack.</param>
        ''' <returns>A System.Drawing.Rectangle containing the coordinates of the packed item.</returns>
        ''' <exception cref="InvalidOperationException">Occurs if the item is larger than the available TexturePacker area</exception>
        ''' <exception cref="ArgumentException">Occurs if the item already exists in the TexturePacker.</exception>
        Friend Function Add(boundingBox As RectangleF) As Rectangle
            Dim bbox As Rectangle = New Rectangle(CInt(Math.Floor(boundingBox.X)), CInt(Math.Floor(boundingBox.Y)),
                                                  CInt(Math.Floor(boundingBox.Width + 0.5F)), CInt(Math.Floor(boundingBox.Height + 0.5F)))
            Return Add(bbox)
        End Function
        ''' <summary> Discards all packed items. </summary>
        Friend Sub Clear()
            root.Clear()
        End Sub
#End Region

        Friend Class Node
            Friend Sub New()
            End Sub

            Private leftField, rightField As Node
            Private rect As Rectangle
            Private occupied As Boolean

            Friend Property Rectangle As Rectangle
                Get
                    Return rect
                End Get
                Set(value As Rectangle)
                    rect = value
                End Set
            End Property

            Friend Property Left As Node
                Get
                    Return leftField
                End Get
                Set(value As Node)
                    leftField = value
                End Set
            End Property

            Friend Property Right As Node
                Get
                    Return rightField
                End Get
                Set(value As Node)
                    rightField = value
                End Set
            End Property

            Friend ReadOnly Property Leaf As Boolean
                Get
                    Return leftField Is Nothing AndAlso rightField Is Nothing
                End Get
            End Property

            Friend Function Insert(bbox As Rectangle) As Node
                If Not Leaf Then
                    ' Recurse towards left child, and if that fails, towards the right.
                    Dim new_node As Node = leftField.Insert(bbox)
                    Return If(new_node, rightField.Insert(bbox))
                Else
                    ' We have recursed to a leaf.

                    ' If it is not empty go back.
                    If occupied Then Return Nothing

                    ' If this leaf is too small go back.
                    If rect.Width < bbox.Width OrElse rect.Height < bbox.Height Then Return Nothing


                    ' If this leaf is the right size, insert here.
                    If rect.Width = bbox.Width AndAlso rect.Height = bbox.Height Then
                        occupied = True
                        Return Me
                    End If


                    ' This leaf is too large, split it up. We'll decide which way to split
                    ' by checking the width and height difference between this rectangle and
                    ' out item's bounding box. If the width difference is larger, we'll split
                    ' horizontaly, else verticaly.
                    leftField = New Node()
                    rightField = New Node()
                    Dim dw As Integer = rect.Width - bbox.Width + 1
                    Dim dh As Integer = rect.Height - bbox.Height + 1

                    If dw > dh Then
                        leftField.rect = New Rectangle(rect.Left, rect.Top, bbox.Width, rect.Height)
                        rightField.rect = New Rectangle(rect.Left + bbox.Width, rect.Top, rect.Width - bbox.Width, rect.Height)
                    Else
                        leftField.rect = New Rectangle(rect.Left, rect.Top, rect.Width, bbox.Height)
                        rightField.rect = New Rectangle(rect.Left, rect.Top + bbox.Height, rect.Width, rect.Height - bbox.Height)
                    End If

                    Return leftField.Insert(bbox)
                End If
            End Function

            Friend Sub Clear()
                If leftField IsNot Nothing Then leftField.Clear()
                If rightField IsNot Nothing Then rightField.Clear()
                leftField = Nothing
                rightField = Nothing
            End Sub
        End Class
    End Class

    Friend Class GlyphSheet(Of T As Texture2D)
        Implements IDisposable

        Private ReadOnly textureField As T
        Private ReadOnly packerField As GlyphPacker
        Private disposed As Boolean
        ''' <summary> A étudier de plus près pour d'autres types de création </summary>
        ''' <param name="width"></param>
        ''' <param name="height"></param>
        Friend Sub New(width As Integer, height As Integer)
            textureField = CType(GetType(T).GetConstructor((New Type() {GetType(Integer), GetType(Integer)})).Invoke(New Object() {width, height}), T)
            'texture.MagnificationFilter = TextureMagFilter.Nearest;
            'texture.MinificationFilter = TextureMinFilter.Nearest;
            packerField = New GlyphPacker(width, height)
        End Sub

        Friend ReadOnly Property Texture As T
            Get
                Return textureField
            End Get
        End Property

        Friend ReadOnly Property Packer As GlyphPacker
            Get
                Return packerField
            End Get
        End Property

        Friend Sub Dispose() Implements IDisposable.Dispose
            If Not disposed Then
                textureField.Dispose()
                disposed = True
            End If
        End Sub
    End Class

    Friend Structure CachedGlyphInfo
        Friend ReadOnly Texture As Texture2D
        Friend ReadOnly RectangleNormalized As RectangleF

        Friend ReadOnly Property Rectangle As Rectangle
            Get
                Return New Rectangle(CInt(Math.Floor(RectangleNormalized.X * Texture.Width)),
                                     CInt(Math.Floor(RectangleNormalized.Y * Texture.Height)),
                                     CInt(Math.Floor(RectangleNormalized.Width * Texture.Width)),
                                     CInt(Math.Floor(RectangleNormalized.Height * Texture.Height)))
            End Get
        End Property


        ' Rect denotes the absolute position of the glyph in the texture [0, Texture.Width], [0, Texture.Height].
        Friend Sub New(texture As Texture2D, rect As Rectangle)
            Me.Texture = texture
            RectangleNormalized = New RectangleF(rect.X / CSng(texture.Width), rect.Y / CSng(texture.Height), rect.Width / CSng(texture.Width), rect.Height / CSng(texture.Height))
        End Sub
    End Structure

    ''' <summary>
    ''' Represents a single character of a specific Font.
    ''' </summary>
    Friend Structure GlyphPackable
        Implements IPackable(Of GlyphPackable)

        Private characterField As Char
        Private fontField As Font
        Private sizeField As SizeF

        ' Constructs a new Glyph that represents the given character and Font.
        Friend Sub New(c As Char, f As Font, s As SizeF)
            If f Is Nothing Then Throw New ArgumentNullException("f", "You must specify a valid font")
            characterField = c
            fontField = f
            sizeField = s
        End Sub

        ''' <summary> Gets the character represented by this Glyph. </summary>
        Friend Property Character As Char
            Get
                Return characterField
            End Get
            Private Set(value As Char)
                characterField = value
            End Set
        End Property
        ''' <summary> Gets the Font of this Glyph. </summary>
        Friend Property Font As Font
            Get
                Return fontField
            End Get
            Private Set(value As Font)
                If value Is Nothing Then Throw New ArgumentNullException("Font", "Glyph font cannot be null")
                fontField = value
            End Set
        End Property
        ''' <summary> Checks whether the given object is equal (memberwise) to the current Glyph. </summary>
        ''' <param name="obj">The obj to check.</param>
        ''' <returns>True, if the object is identical to the current Glyph.</returns>
        Overrides Function Equals(obj As Object) As Boolean
            If TypeOf obj Is GlyphPackable Then Return Equals(CType(obj, GlyphPackable))
            Return False
        End Function
        ''' <summary>  Describes this Glyph object. </summary>
        ''' <returns>Returns a System.String describing this Glyph.</returns>
        Overrides Function ToString() As String
            Return String.Format("'{0}', {1} {2}, {3} {4}, ({5}, {6})", Character, Font.Name, fontField.Style, fontField.Size, fontField.Unit, Width, Height)
        End Function
        ''' <summary> Calculates the hashcode for this Glyph. </summary>
        ''' <returns>A System.Int32 containing a hashcode that uniquely identifies this Glyph.</returns>
        Overrides Function GetHashCode() As Integer
            Return characterField.GetHashCode() Xor fontField.GetHashCode() Xor sizeField.GetHashCode()
        End Function
        ''' <summary> Gets the size of this Glyph. </summary>
        Friend ReadOnly Property Size As SizeF
            Get
                Return sizeField
            End Get
        End Property
        ''' <summary> Gets the bounding box of this Glyph. </summary>
        Friend ReadOnly Property Rectangle As RectangleF
            Get
                Return New RectangleF(PointF.Empty, Size)
            End Get
        End Property
#Region "--- IPackable<T> Members ---"
        ''' <summary> Gets an integer representing the width of the Glyph in pixels. </summary>
        Friend ReadOnly Property Width As Integer Implements IPackable(Of GlyphPackable).Width
            Get
                Return CInt(Math.Ceiling(sizeField.Width))
            End Get
        End Property
        ''' <summary>
        ''' Gets an integer representing the height of the Glyph in pixels.
        ''' </summary>
        Friend ReadOnly Property Height As Integer Implements IPackable(Of GlyphPackable).Height
            Get
                Return CInt(Math.Ceiling(sizeField.Height))
            End Get
        End Property
#End Region
#Region "--- IEquatable<Glyph> Members ---"
        ''' <summary> Compares the current Glyph with the given Glyph. </summary>
        ''' <param name="other">The Glyph to compare to.</param>
        ''' <returns>True if both Glyphs represent the same character of the same Font, false otherwise.</returns>
        Friend Overloads Function Equals(other As GlyphPackable) As Boolean Implements IEquatable(Of GlyphPackable).Equals
            Return Character = other.Character AndAlso Font Is other.Font AndAlso Size = other.Size
        End Function
#End Region
    End Structure

    Friend Structure GlyphEQuatable
        Implements IEquatable(Of GlyphEQuatable)

        Private characterField As Char
        Private fontField As Font
        ''' <summary> Constructs a new Glyph that represents the given character and Font. </summary>
        ''' <param name="c">The character to represent.</param>
        ''' <param name="font">The Font of the character.</param>
        Friend Sub New(c As Char, font As Font)
            If font Is Nothing Then Throw New ArgumentNullException("font")
            characterField = c
            fontField = font
        End Sub

        ''' <summary> Gets the character represented by this Glyph. </summary>
        Friend Property Character As Char
            Get
                Return characterField
            End Get
            Private Set(value As Char)
                characterField = value
            End Set
        End Property
        ''' <summary> Gets the Font of this Glyph. </summary>
        Friend Property Font As Font
            Get
                Return fontField
            End Get
            Private Set(value As Font)
                If value Is Nothing Then Throw New ArgumentNullException("Font", "Glyph font cannot be null")
                fontField = value
            End Set
        End Property

        Friend ReadOnly Property IsWhiteSpace As Boolean
            Get
                Return Char.IsWhiteSpace(Character)
            End Get
        End Property
        ''' <summary> Checks whether the given object is equal (memberwise) to the current Glyph. </summary>
        ''' <param name="obj">The obj to check.</param>
        ''' <returns>True, if the object is identical to the current Glyph.</returns>
        Overrides Function Equals(obj As Object) As Boolean
            If TypeOf obj Is GlyphEQuatable Then Return Equals(CType(obj, GlyphEQuatable))
            Return False
        End Function
        ''' <summary> Describes this Glyph object. </summary>
        ''' <returns>Returns a System.String describing this Glyph.</returns>
        Overrides Function ToString() As String
            Return String.Format("'{0}', {1} {2}, {3} {4}", Character, Font.Name, fontField.Style, fontField.Size, fontField.Unit)
        End Function
        ''' <summary> Calculates the hashcode for this Glyph. </summary>
        ''' <returns>A System.Int32 containing a hashcode that uniquely identifies this Glyph.</returns>
        Public Overrides Function GetHashCode() As Integer
            Return characterField.GetHashCode() Xor fontField.GetHashCode()
        End Function
#Region "IEquatable<Glyph> Members"
        Friend Overloads Function Equals(other As GlyphEQuatable) As Boolean Implements IEquatable(Of GlyphEQuatable).Equals
            Return Character = other.Character AndAlso Font Is other.Font
        End Function
#End Region
    End Structure

    Friend NotInheritable Class GdiPlusGlyphRasterizer
        Implements IGlyphRasterizer

#Region "Fields"
        ' Note: as an optimization, we store the TextBlock hashcode instead of the TextBlock itself.
        Private block_cache As Dictionary(Of Integer, TextExtents) = New Dictionary(Of Integer, TextExtents)()
        Private ReadOnly graphics As Graphics = Graphics.FromImage(New Bitmap(1, 1))
        Private ReadOnly regions As IntPtr() = New IntPtr(MaxMeasurableCharacterRanges - 1) {}
        Private ReadOnly characterRanges As CharacterRange() = New CharacterRange(MaxMeasurableCharacterRanges - 1) {}
        Private glyph_surface As Bitmap
        Private glyph_renderer As Drawing.Graphics
        Private ReadOnly measured_glyphs As List(Of RectangleF) = New List(Of RectangleF)(256)
        Private ReadOnly text_extents_pool As ObjectPool(Of PoolableTextExtents) = New ObjectPool(Of PoolableTextExtents)()

        ' Check the constructor, too, for additional flags.
        ' Used for measuring text. Can set the leftToRight, rightToLeft, vertical and measure trailing spaces flags.
        Private ReadOnly measure_string_format As StringFormat = New StringFormat(StringFormat.GenericDefault)
        Private ReadOnly measure_string_format_tight As StringFormat = New StringFormat(StringFormat.GenericTypographic)
        ' Used for loading glyphs. Only use leftToRight!
        Private ReadOnly load_glyph_string_format As StringFormat = New StringFormat(StringFormat.GenericDefault)
        Private ReadOnly load_glyph_string_format_tight As StringFormat = New StringFormat(StringFormat.GenericTypographic)
        Private Shared ReadOnly newline_characters As Char() = New Char() {Convert.ToChar(10), Convert.ToChar(13)}
        Private Shared ReadOnly MaximumGraphicsClipSize As SizeF
#End Region
#Region "Constructors"
        Shared Sub New()
            Using bmp As Bitmap = New Bitmap(1, 1)
                Using gfx As Graphics = Graphics.FromImage(bmp)
                    MaximumGraphicsClipSize = gfx.ClipBounds.Size
                End Using
            End Using
        End Sub

        Friend Sub New()
            measure_string_format.FormatFlags = measure_string_format.FormatFlags Or StringFormatFlags.MeasureTrailingSpaces Or StringFormatFlags.NoClip
            measure_string_format_tight.FormatFlags = measure_string_format_tight.FormatFlags Or StringFormatFlags.MeasureTrailingSpaces
        End Sub
#End Region
#Region "IGlyphRasterizer Members"
        Friend Function Rasterize(glyph As GlyphEQuatable) As Bitmap Implements IGlyphRasterizer.Rasterize
            Return Rasterize(glyph, TextQuality.Default)
        End Function
        Friend Function Rasterize(glyph As GlyphEQuatable, quality As TextQuality) As Bitmap Implements IGlyphRasterizer.Rasterize
            EnsureSurfaceSize(glyph_surface, glyph_renderer, glyph.Font)
            SetTextRenderingOptions(glyph_renderer, glyph.Font, quality)
            Dim r2 As RectangleF = New RectangleF()
            glyph_renderer.Clear(Color.Transparent)
            glyph_renderer.DrawString(glyph.Character.ToString(), glyph.Font, Brushes.White, Point.Empty, If(glyph.Font.Style = FontStyle.Italic, load_glyph_string_format, load_glyph_string_format_tight)) 'new Point(glyph_surface.Width, 0),
            r2 = FindEdges(glyph_surface)
            Return glyph_surface.Clone(r2, Imaging.PixelFormat.Format32bppArgb)
        End Function
        Friend Function MeasureText(ByRef block As TextBlock) As TextExtents Implements IGlyphRasterizer.MeasureText
            Return MeasureText(block, TextQuality.Default)
        End Function

        Public Function MeasureText(ByRef block As TextBlock, quality As TextQuality) As TextExtents Implements IGlyphRasterizer.MeasureText
            ' First, check if we have cached this text block. Do not use block_cache.TryGetValue, to avoid thrashing
            ' the user's TextBlockExtents struct.
            Dim hashcode As Integer = block.GetHashCode()
            If block_cache.ContainsKey(hashcode) Then Return block_cache(hashcode)

            ' If this block is not cached, we have to measure it and (potentially) place it in the cache.
            Dim extents As TextExtents = MeasureTextExtents(block, quality)
            If (block.Options And TextPrinterOptions.NoCache) = 0 Then block_cache.Add(hashcode, extents)
            Return extents
        End Function

        Friend Sub Clear()
            block_cache.Clear()
        End Sub

        Private Sub EnsureSurfaceSize(ByRef bmp As Bitmap, ByRef gfx As Drawing.Graphics, font As Font)
            If bmp Is Nothing OrElse bmp.Width < 2 * font.Size OrElse bmp.Height < 2 * font.Size Then
                If bmp IsNot Nothing Then bmp.Dispose()
                If gfx IsNot Nothing Then gfx.Dispose()
                bmp = New Bitmap(CInt(2 * font.Size), CInt(2 * font.Size))
                gfx = Drawing.Graphics.FromImage(bmp)
            End If
        End Sub
        ' Modify rendering settings (antialiasing, grid fitting) to improve appearance.
        Private Sub SetTextRenderingOptions(gfx As Drawing.Graphics, font As Font, quality As TextQuality)
            Select Case quality
                Case TextQuality.Default
                    gfx.TextRenderingHint = TextRenderingHint.SystemDefault
                Case TextQuality.High
                    gfx.TextRenderingHint = TextRenderingHint.ClearTypeGridFit
                Case TextQuality.Medium

                    If font.Size <= 18.0F Then
                        gfx.TextRenderingHint = TextRenderingHint.AntiAliasGridFit
                    Else
                        gfx.TextRenderingHint = TextRenderingHint.AntiAlias
                    End If

                Case TextQuality.Low

                    If font.Size <= 18.0F Then
                        gfx.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit
                    Else
                        gfx.TextRenderingHint = TextRenderingHint.SingleBitPerPixel
                    End If
            End Select
        End Sub
        Private Function MeasureTextExtents(ByRef block As TextBlock, quality As TextQuality) As TextExtents
            ' Todo: Parse layout options:
            Dim format As StringFormat = If(block.Font.Italic, measure_string_format, measure_string_format_tight)

            'StringFormat format = measure_string_format_tight;

            If block.Direction = TextDirection.Vertical Then
                format.FormatFlags = format.FormatFlags Or StringFormatFlags.DirectionVertical
            Else
                format.FormatFlags = format.FormatFlags And Not StringFormatFlags.DirectionVertical
            End If

            If block.Direction = TextDirection.RightToLeft Then
                format.FormatFlags = format.FormatFlags Or StringFormatFlags.DirectionRightToLeft
            Else
                format.FormatFlags = format.FormatFlags And Not StringFormatFlags.DirectionRightToLeft
            End If

            If block.Alignment = TextAlignment.Near Then
                format.Alignment = StringAlignment.Near
            ElseIf block.Alignment = TextAlignment.Center Then
                format.Alignment = StringAlignment.Center
            Else
                format.Alignment = StringAlignment.Far
            End If

            Dim extents As TextExtents = text_extents_pool.Acquire()
            Dim rect As RectangleF = block.Bounds
            ' Work around Mono/GDI+ bug, which causes incorrect
            ' text wraping when block.Bounds == SizeF.Empty.
            If block.Bounds.Size = SizeF.Empty Then
                rect.Size = MaximumGraphicsClipSize
            End If
            SetTextRenderingOptions(graphics, block.Font, quality)
            Dim native_graphics As IntPtr = GetNativeGraphics(graphics)
            Dim native_font As IntPtr = GetNativeFont(block.Font)
            Dim native_string_format As IntPtr = GetNativeStringFormat(format)
            Dim max_width As Single = 0, max_height As Single = 0


            ' It seems that the mere presence of \n and \r characters
            ' is enough for Mono to botch the layout (even if these
            ' characters are not processed.) We'll need to find a
            ' different way to perform layout on Mono, probably
            ' through Pango.
            ' Todo: This workaround  allocates memory.
            'if (Configuration.RunningOnMono)
            If True Then
                Dim lines As String() = block.Text.Replace(Convert.ToChar(13), String.Empty).Split(Convert.ToChar(10))

                For Each s As String In lines
                    Dim width, height As Single
                    extents.AddRange(MeasureGlyphExtents(block, s, native_graphics, native_font, native_string_format, rect, width, height))

                    If (block.Direction And TextDirection.Vertical) = 0 Then
                        rect.Y += block.Font.Height
                    Else
                        rect.X += block.Font.Height
                    End If

                    If width > max_width Then max_width = width
                    If height > max_height Then max_height = height
                Next
            End If

            If extents.Count > 0 Then
                extents.BoundingBox = New RectangleF(extents(0).X, extents(0).Y, max_width, max_height)
            Else
                extents.BoundingBox = RectangleF.Empty
            End If

            Return extents
        End Function
        ' Gets the bounds of each character in a line of text.
        ' Each line is processed in blocks of 32 characters (GdiPlus.MaxMeasurableCharacterRanges).
        Private Function MeasureGlyphExtents(ByRef block As TextBlock, text As String, native_graphics As IntPtr,
                                             native_font As IntPtr, native_string_format As IntPtr, ByRef layoutRect As RectangleF,
                                             ByRef max_width As Single, ByRef max_height As Single) As IEnumerable(Of RectangleF)
            measured_glyphs.Clear()
            max_width = layoutRect.Left
            max_height = layoutRect.Top
            Dim last_line_width As Single = 0, last_line_height As Single = 0
            Dim current As Integer = 0

            While current < text.Length
                Dim num_characters As Integer = If(text.Length - current > MaxMeasurableCharacterRanges, MaxMeasurableCharacterRanges, text.Length - current)
                Dim status As Integer = 0


                ' Prepare the character ranges and region structs for the measurement.
                For i As Integer = 0 To num_characters - 1
                    If text(current + i) = Convert.ToChar(10) OrElse text(current + i) = Convert.ToChar(13) Then
                        Throw New NotSupportedException()
                    End If
                    characterRanges(i) = New CharacterRange(current + i, 1)
                    Dim region As IntPtr
                    status = CreateRegion(region)
                    regions(i) = region
                    Debug.Assert(status = 0, String.Format("GDI+ error: {0}", status))
                Next

                status = SetStringFormatMeasurableCharacterRanges(native_string_format, num_characters, characterRanges)
                Debug.Assert(status = 0, String.Format("GDI+ error: {0}", status))
                status = MeasureCharacterRanges(native_graphics, text, text.Length, native_font, layoutRect, native_string_format, num_characters, regions)
                Debug.Assert(status = 0, String.Format("GDI+ error: {0}", status))


                ' Read back the results of the measurement.
                For i As Integer = 0 To num_characters - 1
                    Dim rect As RectangleF = New RectangleF()
                    GetRegionBounds(regions(i), native_graphics, rect)
                    Debug.Assert(status = 0, String.Format("GDI+ error: {0}", status))
                    DeleteRegion(regions(i))
                    Debug.Assert(status = 0, String.Format("GDI+ error: {0}", status))
                    If rect.Bottom > max_height Then max_height = rect.Bottom
                    If rect.Right > max_width Then max_width = rect.Right
                    If rect.X > last_line_width Then last_line_width = rect.X
                    If rect.Y > last_line_height Then last_line_height = rect.Y
                    measured_glyphs.Add(rect)
                Next

                current += num_characters
            End While


            ' Make sure the current height is updated, if the the current line has wrapped due to word-wraping.
            ' Otherwise, the next line will overlap with the current one.
            If measured_glyphs.Count > 1 Then

                If (block.Direction And TextDirection.Vertical) = 0 Then
                    If layoutRect.Y < last_line_height Then layoutRect.Y = last_line_height
                Else
                    If layoutRect.X < last_line_width Then layoutRect.X = last_line_width
                End If
            End If


            ' Mono's GDI+ implementation suffers from an issue where the specified layoutRect is not taken into
            ' account. We will try to improve the situation by moving text to the correct location on this
            ' error condition. This will not help word wrapping, but it is better than nothing.
            ' Todo: Mono 2.8 is supposed to ship with a Pango-based GDI+ text renderer, which should not
            ' suffer from this bug. Verify that this is the case and remove the hack.
            If OpenTK.Configuration.RunningOnMono AndAlso (layoutRect.X <> 0 OrElse layoutRect.Y <> 0) AndAlso measured_glyphs.Count > 0 Then

                For i As Integer = 0 To measured_glyphs.Count - 1
                    Dim rect As RectangleF = measured_glyphs(i)
                    rect.X += layoutRect.X
                    rect.Y += layoutRect.Y
                    measured_glyphs(i) = rect
                Next
            End If

            Return measured_glyphs
        End Function
#Region "FindEdges"
        ' Note: The bool parameter is not used at this point.
        ' We might need it if we ever load true rightToLeft glyphs.
        Private Function FindEdges(bmp As Bitmap) As Rectangle
            Dim data As BitmapData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, Imaging.PixelFormat.Format32bppArgb)
            Dim rect As Rectangle = Rectangle.FromLTRB(0, 0, FindRightEdge(bmp, data.Scan0), FindBottomEdge(bmp, data.Scan0))
            bmp.UnlockBits(data)
            Return rect
        End Function
        Private Const NbOctetsParPixel As Integer = 4
        Private Function FindRightEdge(Bmp As Bitmap, Scan0 As IntPtr) As Integer
            Dim Stride As Integer = NbOctetsParPixel * Bmp.Width
            For X As Integer = Stride - 1 To NbOctetsParPixel - 1 Step -NbOctetsParPixel
                For Y As Integer = 0 To (Bmp.Height - 1) * Stride Step Stride
                    If Marshal.ReadByte(Scan0, Y + X) > 0 Then
                        Return (X \ NbOctetsParPixel) + 1
                    End If
                Next
            Next
            Return 0
        End Function

        Private Function FindBottomEdge(Bmp As Bitmap, Scan0 As IntPtr) As Integer
            Dim Stride As Integer = NbOctetsParPixel * Bmp.Width
            For Y As Integer = (Bmp.Height - 1) * Stride To 0 Step -Stride
                For X As Integer = NbOctetsParPixel - 1 To Stride - 1 Step NbOctetsParPixel
                    If Marshal.ReadByte(Scan0, Y + X) > 0 Then
                        Return (Y \ Stride) + 1
                    End If
                Next
            Next
            Return 0
        End Function

        Private Sub Clear1() Implements IGlyphRasterizer.Clear
            Throw New NotImplementedException()
        End Sub
#End Region
#End Region
    End Class

    Friend Class GlyphEnumerator
        Implements IEnumerator(Of GlyphEQuatable)

        Private ReadOnly text As String
        Private ReadOnly font As Font
        Private implementation As IEnumerator(Of Char)

        Friend Sub New(text As String, font As Font)
            If Equals(text, Nothing) Then Throw New ArgumentNullException("text")
            If font Is Nothing Then Throw New ArgumentNullException("font")
            Me.text = text
            Me.font = font
            implementation = text.GetEnumerator()
        End Sub

        Friend ReadOnly Property Current As GlyphEQuatable Implements IEnumerator(Of GlyphEQuatable).Current
            Get
                Return New GlyphEQuatable(implementation.Current, font)
            End Get
        End Property

        Friend Sub Dispose() Implements IDisposable.Dispose
            implementation.Dispose()
        End Sub
#Region "IEnumerator Members"
        Private ReadOnly Property CurrentProp As Object Implements IEnumerator.Current
            Get
                Return New GlyphEQuatable(implementation.Current, font)
            End Get
        End Property

        Friend Function MoveNext() As Boolean Implements IEnumerator.MoveNext
            Dim status As Boolean

            Do
                status = implementation.MoveNext()
            Loop While status AndAlso (implementation.Current = Convert.ToChar(13) OrElse implementation.Current = Convert.ToChar(10))

            Return status
        End Function

        Friend Sub Reset() Implements IEnumerator.Reset
            implementation.Reset()
        End Sub
#End Region
    End Class

    Friend Structure TextBlock
        Implements IEquatable(Of TextBlock), IEnumerable(Of GlyphEQuatable)

#Region "Fields"
        Friend ReadOnly Text As String
        Friend ReadOnly Font As Font
        Friend ReadOnly Bounds As RectangleF
        Friend ReadOnly Options As TextPrinterOptions
        Friend ReadOnly Alignment As TextAlignment
        Friend ReadOnly Direction As TextDirection
        Friend ReadOnly UsageCount As Integer
#End Region

        Friend Sub New(text As String, font As Font, bounds As RectangleF, options As TextPrinterOptions, alignment As TextAlignment, direction As TextDirection)
            Me.Text = text
            Me.Font = font
            Me.Bounds = bounds
            Me.Options = options
            Me.Alignment = alignment
            Me.Direction = direction
            UsageCount = 0
        End Sub
#Region "Public Members"
        Overrides Function Equals(obj As Object) As Boolean
            If Not (TypeOf obj Is TextBlock) Then Return False
            Return Equals(CType(obj, TextBlock))
        End Function

        Overrides Function GetHashCode() As Integer
            Return Text.GetHashCode() Xor Font.GetHashCode() Xor Bounds.GetHashCode() Xor Options.GetHashCode()
        End Function

        Default Friend ReadOnly Property Item(i As Integer) As GlyphEQuatable
            Get
                Return New GlyphEQuatable(Text(i), Font)
            End Get
        End Property
#End Region

#Region "IEquatable<TextBlock> Members"
        Friend Overloads Function Equals(other As TextBlock) As Boolean Implements IEquatable(Of TextBlock).Equals
            Return Equals(Text, other.Text) AndAlso Font Is other.Font AndAlso Bounds = other.Bounds AndAlso Options = other.Options
        End Function
#End Region
#Region "IEnumerable<Glyph> Members"
        Friend Function GetEnumerator() As IEnumerator(Of GlyphEQuatable) Implements IEnumerable(Of GlyphEQuatable).GetEnumerator
            Return New GlyphEnumerator(Text, Font)
        End Function
#End Region
#Region "IEnumerable Members"
        Private Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
            Return New GlyphEnumerator(Text, Font)
        End Function
#End Region
    End Structure

    Friend Class TextBlockComparer
        Implements IComparer(Of TextBlock)

        Friend Sub New()
        End Sub

        Friend Function Compare(x As TextBlock, y As TextBlock) As Integer Implements IComparer(Of TextBlock).Compare
            Return x.UsageCount.CompareTo(y.UsageCount)
        End Function
    End Class

    Friend Class PoolableTextExtents
        Inherits TextExtents
        Implements IPoolableType(Of PoolableTextExtents)

        Private ownerField As ObjectPool(Of PoolableTextExtents)

        Private Property Owner As ObjectPool(Of PoolableTextExtents) Implements IPoolableType(Of PoolableTextExtents).Owner
            Get
                Return ownerField
            End Get
            Set(value As ObjectPool(Of PoolableTextExtents))
                ownerField = value
            End Set
        End Property

        Private Sub OnAcquire() Implements IPoolable.OnAcquire
            Clear()
        End Sub

        Private Sub OnRelease() Implements IPoolable.OnRelease
        End Sub
    End Class

    ''' <summary> Defines available options for the TextPrinter. </summary>
    <Flags>
    Friend Enum TextPrinterOptions
        ''' <summary>The TextPrinter will use default printing options.</summary>
        [Default] = &H0
        ''' <summary>The TextPrinter will not cache text blocks as they are measured or printed.</summary>
        NoCache = &H1
    End Enum

    ''' <summary>  Defines available alignments for text. </summary>
    Friend Enum TextAlignment
        ''' <summary>The text is aligned to the near side (left for left-to-right text and right for right-to-left text).</summary>
        Near = 0
        ''' <summary>The text is aligned to the center.</summary>
        Center
        ''' <summary>The text is aligned to the far side (right for left-to-right text and left for right-to-left text).</summary>
        Far
    End Enum

    ''' <summary> Defines available directions for text layout. </summary>
    Friend Enum TextDirection
        ''' <summary>The text is layed out from left to right.</summary>
        LeftToRight
        ''' <summary>The text is layed out from right to left.</summary>
        RightToLeft
        ''' <summary>The text is layed out vertically.</summary>
        Vertical
    End Enum

    ''' <summary> Defines available quality levels for text printing. </summary>
    Friend Enum TextQuality
        ''' <summary>Use the default quality, as specified by the operating system.</summary>
        [Default] = 0
        ''' <summary>Use fast, low quality text (typically non-antialiased) .</summary>
        Low
        ''' <summary>Use medium quality text (typically grayscale antialiased).</summary>
        Medium
        ''' <summary>Use slow, high quality text (typically subpixel antialiased).</summary>
        High
    End Enum

    ''' <summary> Represents an item that can be packed with the TexturePacker. </summary>
    ''' <typeparam name="T"> The type of the packable item. </typeparam>
    Friend Interface IPackable(Of T)
        Inherits IEquatable(Of T)

        ReadOnly Property Width As Integer
        ReadOnly Property Height As Integer
    End Interface

    Friend Interface IPoolable
        Inherits IDisposable

        Sub OnAcquire()
        Sub OnRelease()
    End Interface

    Friend Interface IPoolableType(Of T As {IPoolableType(Of T), New})
        Inherits IPoolable

        Property Owner As ObjectPool(Of T)
    End Interface

    ''' <summary> Defines the interface for a TextPrinter. </summary>
    Friend Interface ITextPrinter
        Inherits IDisposable
#Region "Print"
        ''' <summary> Prints text using the specified color and layout options. </summary>
        ''' <param name="text">The System.String to print.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to print text.</param>
        ''' <param name="color">The System.Drawing.Color that will be used to print text.</param>
        Sub Ecrire(text As String, font As Font, color As Color)

        ''' <summary> Prints text using the specified color and layout options. </summary>
        ''' <param name="text">The System.String to print.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to print text.</param>
        ''' <param name="color">The System.Drawing.Color that will be used to print text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        Sub Ecrire(text As String, font As Font, color As Color, rect As RectangleF)

        ''' <summary> Prints text using the specified color and layout options. </summary>
        ''' <param name="text">The System.String to print.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to print text.</param>
        ''' <param name="color">The System.Drawing.Color that will be used to print text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to print text.</param>
        Sub Ecrire(text As String, font As Font, color As Color, rect As RectangleF, options As TextPrinterOptions)

        ''' <summary> Prints text using the specified color and layout options. </summary>
        ''' <param name="text">The System.String to print.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to print text.</param>
        ''' <param name="color">The System.Drawing.Color that will be used to print text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to print text.</param>
        ''' <param name="alignment">The OpenTK.Graphics.TextAlignment that will be used to print text.</param>
        Sub Ecrire(text As String, font As Font, color As Color, rect As RectangleF, options As TextPrinterOptions, alignment As TextAlignment)

        ''' <summary> Prints text using the specified color and layout options. </summary>
        ''' <param name="text">The System.String to print.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to print text.</param>
        ''' <param name="color">The System.Drawing.Color that will be used to print text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to print text.</param>
        ''' <param name="alignment">The OpenTK.Graphics.TextAlignment that will be used to print text.</param>
        ''' <param name="direction">The OpenTK.Graphics.TextDirection that will be used to print text.</param>
        Sub Ecrire(text As String, font As Font, color As Color, rect As RectangleF, options As TextPrinterOptions, alignment As TextAlignment, direction As TextDirection)
#End Region
#Region "Measure"
        ''' <summary> Measures text using the specified layout options. </summary>
        ''' <param name="text">The System.String to measure.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to measure text.</param>
        ''' <returns>An OpenTK.Graphics.TextExtents instance that contains the results of the measurement.</returns>
        Function Mesurer(text As String, font As Font) As TextExtents

        ''' <summary> Measures text using the specified layout options. </summary>
        ''' <param name="text">The System.String to measure.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to measure text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <returns>An OpenTK.Graphics.TextExtents instance that contains the results of the measurement.</returns>
        Function Mesurer(text As String, font As Font, rect As RectangleF) As TextExtents

        ''' <summary> Measures text using the specified layout options. </summary>
        ''' <param name="text">The System.String to measure.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to measure text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to measure text.</param>
        ''' <returns>An OpenTK.Graphics.TextExtents instance that contains the results of the measurement.</returns>
        Function Mesurer(text As String, font As Font, rect As RectangleF, options As TextPrinterOptions) As TextExtents

        ''' <summary> Measures text using the specified layout options. </summary>
        ''' <param name="text">The System.String to measure.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to measure text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to measure text.</param>
        ''' <param name="alignment">The OpenTK.Graphics.TextAlignment that will be used to measure text.</param>
        ''' <returns>An OpenTK.Graphics.TextExtents instance that contains the results of the measurement.</returns>
        Function Mesurer(text As String, font As Font, rect As RectangleF, options As TextPrinterOptions, alignment As TextAlignment) As TextExtents

        ''' <summary> Measures text using the specified layout options. </summary>
        ''' <param name="text">The System.String to measure.</param>
        ''' <param name="font">The System.Drawing.Font that will be used to measure text.</param>
        ''' <param name="rect">The System.Drawing.Rectangle that defines the bounds for text layout.</param>
        ''' <param name="options">The OpenTK.Graphics.TextPrinterOptions that will be used to measure text.</param>
        ''' <param name="alignment">The OpenTK.Graphics.TextAlignment that will be used to measure text.</param>
        ''' <param name="direction">The OpenTK.Graphics.TextDirection that will be used to measure text.</param>
        ''' <returns>An OpenTK.Graphics.TextExtents instance that contains the results of the measurement.</returns>
        Function Mesurer(text As String, font As Font, rect As RectangleF, options As TextPrinterOptions, alignment As TextAlignment, direction As TextDirection) As TextExtents
#End Region
        ''' <summary> Sets up a resolution-dependent orthographic projection. </summary>
        Sub Begin()

        ''' <summary> Restores the projection and modelview matrices to their previous state. </summary>
        Sub [End]()
    End Interface

    Friend Interface ITextOutputProvider
        Inherits IDisposable

        Sub Print(ByRef block As TextBlock, color As Color, rasterizer As IGlyphRasterizer)
        Sub Clear()
        Sub Begin()
        Sub [End]()
    End Interface

    ''' <summary> Defines a common interface to all OpenGL resources.  </summary>
    Friend Interface IGraphicsResource
        Inherits IDisposable

        ''' <summary>
        ''' Gets the GraphicsContext that owns this resource.
        ''' </summary>
        ReadOnly Property Context As IGraphicsContext

        ''' <summary>
        ''' Gets the Id of this IGraphicsResource.
        ''' </summary>
        ReadOnly Property Id As Integer
    End Interface

    Friend Interface IFont
        Inherits IDisposable

        Sub LoadGlyphs(glyphs As String)
        ReadOnly Property Height As Single
        Sub MeasureString(str As String, ByRef width As Single, ByRef height As Single)
    End Interface

    Friend Interface IGdiPlusInternals
        Function GetNativeGraphics(graphics As Drawing.Graphics) As IntPtr
        Function GetNativeFont(font As Font) As IntPtr
        Function GetNativeStringFormat(format As StringFormat) As IntPtr
    End Interface

    Friend Interface IGlyphRasterizer
        Function Rasterize(glyph As GlyphEQuatable) As Bitmap
        Function Rasterize(glyph As GlyphEQuatable, quality As TextQuality) As Bitmap
        Function MeasureText(ByRef block As TextBlock) As TextExtents
        Function MeasureText(ByRef block As TextBlock, quality As TextQuality) As TextExtents
        Sub Clear()
    End Interface

    Friend Interface IGlyphCache
        Inherits IDisposable

        Sub Add(glyph As GlyphEQuatable, rasterizer As IGlyphRasterizer, quality As TextQuality)
        Function Contains(glyph As GlyphEQuatable) As Boolean
        Default ReadOnly Property Item(glyph As GlyphEQuatable) As CachedGlyphInfo
        Sub Clear()
    End Interface
End Namespace
