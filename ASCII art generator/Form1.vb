Imports System.IO
Imports System.Threading

Public Class Form1

    'public variables based on the size of each pixel
    Public pixelSize As Integer
    Public Value As Integer = 0

    'public variable for the source image
    Public sourceImage As Bitmap

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        'when the button is pressed, open file dialog and allow the user to open .jpg or .bmp images. Store it in picturebox 1
        Dim ofd As New OpenFileDialog
        ofd.InitialDirectory = My.Computer.FileSystem.SpecialDirectories.MyPictures
        ofd.Filter = "JPEG files (*.jpg)|*.jpg|Bitmap files (*.bmp)|*.bmp"
        Dim result As DialogResult = ofd.ShowDialog
        If Not (PictureBox1) Is Nothing And ofd.FileName <> String.Empty Then
            sourceImage = Image.FromFile(ofd.FileName)
            PictureBox1.BackgroundImage = sourceImage
            PictureBox1.BackgroundImageLayout = ImageLayout.Zoom
        End If
        pixelSize = Int(TrackBar1.Value)

        'if an image has been loaded, run ImageFunctions and send it the image that has been uploaded

        If Not PictureBox1.BackgroundImage Is Nothing Then ImageFunctions(sourceImage)

    End Sub

    Sub ImageFunctions(ByVal source As Bitmap)

        'local variables for general use in this routine
        Dim asciiart(,) As String
        Dim bmp As Bitmap = source
        Dim finalArt As String

        'run all the subroutines
        bmp = Pixelate(CropToScale(ConvertToGreyscale(bmp)))

        'display the outcome
        PictureBox2.BackgroundImage = bmp
        PictureBox2.BackgroundImageLayout = ImageLayout.Zoom

        'generate the art array
        asciiart = ConvertToText(bmp)

        'put the array into a single string
        For y = 0 To (bmp.Height - 1) / pixelSize
            For x = 0 To (bmp.Width - 1) / pixelSize
                finalArt = finalArt + asciiart(x, y)
            Next
            finalArt += vbNewLine
        Next

        'display the outcome, to be coppied and pasted into notepad
        TextBox1.Text = finalArt

    End Sub


    Function CropToScale(ByVal source As Bitmap) As Bitmap

        'local variables for general use in this routine

        Dim bm As New Bitmap(source)
        'set these variables to the desired width, a multiple of the number of pixels that is a little less than the original size
        Dim newWidth As Integer = bm.Width - (bm.Width Mod pixelSize)
        Dim newHeight As Integer = bm.Height - (bm.Height Mod pixelSize)

        'crop the image to the new dimensions
        Dim CropRect As New Rectangle()
        CropRect.Width = newWidth
        CropRect.Height = newHeight
        Dim OriginalImage = source
        Dim CropImage = New Bitmap(CropRect.Width, CropRect.Height)
        Using grp = Graphics.FromImage(CropImage)
            grp.DrawImage(OriginalImage, New Rectangle(0, 0, CropRect.Width, CropRect.Height), CropRect, GraphicsUnit.Pixel)
            OriginalImage.Dispose()
        End Using

        'return the image with an appropriate size and shape
        Return CropImage

    End Function


    Function ConvertToGreyscale(ByVal source As Bitmap) As Bitmap

        'iterate through each pixel, convert it to greyscale by setting the RGB to the appropriate values
        Dim bm As New Bitmap(source)
        Using fp As New FastPix(bm)
            For y As Integer = 0 To bm.Height - 1
                For x As Integer = 0 To bm.Width - 1
                    Dim c As Color = fp.GetPixel(x, y)
                    Dim luma As Integer = CInt(c.R * 0.3 + c.G * 0.59 + c.B * 0.11)
                    fp.SetPixel(x, y, Color.FromArgb(luma, luma, luma))
                Next
            Next
        End Using

        'returns the greyscale image
        Return bm

    End Function


    Function Pixelate(ByVal source As Bitmap) As Bitmap

        'local variables for general use in this routine
        Dim bm As New Bitmap(source)
        Dim c As Color
        Dim averageRed As Integer
        Dim averageGreen As Integer
        Dim averageBlue As Integer
        Dim x, y As Integer

        'using fastpix to speed it up
        Using fp As New FastPix(bm)

            'iterate through pixels, in blocks the size of variable "pixelSize"
            For yCount = 0 To (source.Height / pixelSize) - 1
                For xCount = 0 To (source.Width / pixelSize) - 1

                    'at each new large pixel, add together the RGB of each old small pixel
                    For y = 0 To pixelSize - 1
                        For x = 0 To pixelSize - 1
                            c = fp.GetPixel(x + xCount * pixelSize, y + yCount * pixelSize)
                            averageRed += c.R
                            averageGreen += c.G
                            averageBlue += c.B
                        Next
                    Next

                    'find the average colour of all of these pixels
                    averageRed /= (x * y)
                    averageGreen /= (x * y)
                    averageBlue /= (x * y)

                    'set the average colour to each pixel in the new pixel block
                    For y = 0 To pixelSize - 1
                        For x = 0 To pixelSize - 1
                            fp.SetPixel(x + xCount * pixelSize, y + yCount * pixelSize, Color.FromArgb(averageRed, averageGreen, averageBlue))
                        Next
                    Next

                    'reset the averages for the next iteration loop
                    averageRed = 0
                    averageGreen = 0
                    averageBlue = 0

                Next
            Next

            'return the pixelated image
            Return bm

        End Using

    End Function

    Function ConvertToText(ByVal source As Bitmap) As String(,)

        'local variables for general use in this routine
        Dim bm As New Bitmap(source)
        Dim brightness As Double
        Dim position As Integer

        'declare the array that will store the ascii art
        Dim asciiArt(bm.Width - (bm.Width Mod pixelSize), bm.Height - (bm.Height Mod pixelSize)) As String

        'declare the lookup table for the gradient, in order from light to dark
        Dim TextTable() As String = {"  ", "..", ",,", "::", ";;", "~~", "--", "++", "ii", "!!", "ll", "II", "??", "rr", "cc", "vv", "uu", "LL", "CC", "JJ", "UU", "YY", "XX", "ZZ", "00", "QQ", "WW", "MM", "BB", "88", "&&", "%%", "$$", "##", "@@"}


        'iterate through each new large pixel, set the corresponding part of the array to the relevant text, based on the brightness
        For y As Integer = 0 To bm.Height - 1 Step pixelSize
            For x As Integer = 0 To bm.Width - 1 Step pixelSize
                brightness = bm.GetPixel(x, y).GetBrightness()
                position = brightness / Convert.ToDouble(1 / TextTable.Length)
                Try
                    asciiArt(x / pixelSize, y / pixelSize) = TextTable(position - 1)
                Catch
                    asciiArt(x / pixelSize, y / pixelSize) = "  "
                End Try
            Next
        Next

        'return the array
        Return asciiArt

    End Function

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles TrackBar1.Scroll

        'when the trackbar is changed set the pixel size to the trackbar position and run the program to turn it to ASCII art
        pixelSize = Int(TrackBar1.Value)
        If Not PictureBox1.BackgroundImage Is Nothing Then ImageFunctions(sourceImage)
    End Sub


End Class
