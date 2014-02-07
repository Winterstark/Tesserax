using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Threading;
using Microsoft.WindowsAPICodePack.Shell;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Runtime.InteropServices;


namespace Tesserax
{
    public partial class Main : Form
    {
        const double VERSION = 1.0;

        //enums
        enum Align { Center, Relative, HoldPosition, Custom }

        #region Classes
        class ImageFile
        {
            public Image smallThumb, largeThumb, img;
            public string path, size, uploadedURL;
            public float zoom;
            public int w, h;
            public bool hasTransparency;


            public ImageFile(string path)
            {
                this.path = path;
            }

            public void Prepare()
            {
                ShellFile shFile = ShellFile.FromFilePath(path);

                zoom = 0;
                w = (int)shFile.Properties.System.Image.HorizontalSize.Value;
                h = (int)shFile.Properties.System.Image.VerticalSize.Value;

                long bytes = (long)shFile.Properties.System.Size.Value;
                if (bytes > 1048576)
                    size = Math.Round((double)bytes / 1048576, 2) + "MB";
                else
                    size = bytes / 1024 + "KB";

                largeThumb = shFile.Thumbnail.LargeBitmap;

                //prepare small thumb for display (crop image, add reflection)
                Bitmap thumb = shFile.Thumbnail.SmallBitmap;

                //determine src rectangle
                int srcX, srcY, srcSize;

                if (thumb.Height > thumb.Width)
                {
                    srcX = 0;
                    srcY = thumb.Height / 2 - thumb.Width / 2;
                    srcSize = thumb.Width;
                }
                else
                {
                    srcX = thumb.Width / 2 - thumb.Height / 2;
                    srcY = 0;
                    srcSize = thumb.Height;
                }

                //draw reflection
                smallThumb = new Bitmap(30, 45);
                Graphics thGfx = Graphics.FromImage(smallThumb);
                thGfx.DrawImage(thumb, new Rectangle(0, 0, 30, 30), new Rectangle(srcX, srcY, srcSize, srcSize), GraphicsUnit.Pixel);
                thGfx.DrawImage(thumb, new Rectangle(0, 29, 30, 15), srcX, srcY + srcSize, srcSize, -srcSize / 2, GraphicsUnit.Pixel, calcTransparentAttribs(64));

                //cleanup
                thGfx.Dispose();
                thumb.Dispose();
            }

            public void DisposeImages()
            {
                if (smallThumb != null)
                    smallThumb.Dispose();
                if (largeThumb != null)
                    largeThumb.Dispose();
                if (img != null)
                    img.Dispose();
            }
        }

        //base class for buttons
        class BaseButton
        {
            protected Image icon;
            protected Rectangle bounds;
            public string Name;
            protected int offset;
            protected Action clickEvent;


            public void Draw(Graphics gfx, bool mouseOver)
            {
                if (!mouseOver)
                    gfx.DrawImage(icon, bounds.Location);
                else
                    drawBrightenedImage(icon, bounds.Location, gfx);
            }

            public bool IsMouseOver(Point mouseLoc)
            {
                return bounds.Contains(mouseLoc);
            }

            public bool ClickIfSelected(Point mouseLoc)
            {
                if (bounds.Contains(mouseLoc))
                {
                    clickEvent();
                    return true;
                }
                else
                    return false;
            }
        }

        //buttons positioned in a horizontal line above the thumbnail strip
        class UIButton : BaseButton
        {
            public UIButton(string name, Image icon, int xOffset, Action clickEvent)
            {
                this.Name = name;
                this.icon = icon;
                this.offset = xOffset;
                this.clickEvent = clickEvent;

                bounds = new Rectangle(0, 0, icon.Width, icon.Height);
            }

            public void Relocate(int xCenter, int y)
            {
                bounds.Location = new Point(xCenter + offset, y + (32 - icon.Height));
            }
        }

        //buttons positioned in a vertical line on the left side
        class ActionButton : BaseButton
        {
            public ActionButton(string name, Image icon, int yOffset, Action clickEvent)
            {
                this.Name = name;
                this.icon = icon;
                this.offset = yOffset;
                this.clickEvent = clickEvent;

                bounds = new Rectangle(0, 0, icon.Width, icon.Height);
            }

            public void Relocate(int yBottom)
            {
                bounds.Location = new Point(8, yBottom + offset + (32 - icon.Height));
            }
        }

        class DelToBin
        {
            /// <summary>
            /// Possible flags for the SHFileOperation method.
            /// </summary>
            [Flags]
            public enum FileOperationFlags : ushort
            {
                /// <summary>
                /// Do not show a dialog during the process
                /// </summary>
                FOF_SILENT = 0x0004,
                /// <summary>
                /// Do not ask the user to confirm selection
                /// </summary>
                FOF_NOCONFIRMATION = 0x0010,
                /// <summary>
                /// Delete the file to the recycle bin.  (Required flag to send a file to the bin
                /// </summary>
                FOF_ALLOWUNDO = 0x0040,
                /// <summary>
                /// Do not show the names of the files or folders that are being recycled.
                /// </summary>
                FOF_SIMPLEPROGRESS = 0x0100,
                /// <summary>
                /// Surpress errors, if any occur during the process.
                /// </summary>
                FOF_NOERRORUI = 0x0400,
                /// <summary>
                /// Warn if files are too big to fit in the recycle bin and will need
                /// to be deleted completely.
                /// </summary>
                FOF_WANTNUKEWARNING = 0x4000,
            }

            /// <summary>
            /// File Operation Function Type for SHFileOperation
            /// </summary>
            public enum FileOperationType : uint
            {
                /// <summary>
                /// Move the objects
                /// </summary>
                FO_MOVE = 0x0001,
                /// <summary>
                /// Copy the objects
                /// </summary>
                FO_COPY = 0x0002,
                /// <summary>
                /// Delete (or recycle) the objects
                /// </summary>
                FO_DELETE = 0x0003,
                /// <summary>
                /// Rename the object(s)
                /// </summary>
                FO_RENAME = 0x0004,
            }

            /// <summary>
            /// SHFILEOPSTRUCT for SHFileOperation from COM
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
            private struct SHFILEOPSTRUCT
            {

                public IntPtr hwnd;
                [MarshalAs(UnmanagedType.U4)]
                public FileOperationType wFunc;
                public string pFrom;
                public string pTo;
                public FileOperationFlags fFlags;
                [MarshalAs(UnmanagedType.Bool)]
                public bool fAnyOperationsAborted;
                public IntPtr hNameMappings;
                public string lpszProgressTitle;
            }

            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

            /// <summary>
            /// Send file to recycle bin
            /// </summary>
            /// <param name="path">Location of directory or file to recycle</param>
            /// <param name="flags">FileOperationFlags to add in addition to FOF_ALLOWUNDO</param>
            private static bool Send(string path, FileOperationFlags flags)
            {
                try
                {
                    var fs = new SHFILEOPSTRUCT
                    {
                        wFunc = FileOperationType.FO_DELETE,
                        pFrom = path + '\0' + '\0',
                        fFlags = FileOperationFlags.FOF_ALLOWUNDO | flags
                    };
                    SHFileOperation(ref fs);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            /// <summary>
            /// Send file to recycle bin.  Display dialog, display warning if files are too big to fit (FOF_WANTNUKEWARNING)
            /// </summary>
            /// <param name="path">Location of directory or file to recycle</param>
            public static bool Send(string path)
            {
                return Send(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_WANTNUKEWARNING);
            }
        }
        #endregion

        #region Global Declarations
        Upload uploadForm;

        BackgroundWorker imgLoader, thumbLoader;
        UIButton[] buttons;
        ActionButton[] sideButtons;

        Align alignment = Align.Relative;
        List<ImageFile> imgFiles;
        Image img, background;
        Image checkers, xButton, xpndButton, exitTessModeButton, yellowStarButton, yellowStar, blackStar, miniStar;
        TextureBrush texture;
        SolidBrush thumbStripBrush;
        Color comicBackground;
        Font zoomDisplayFont, infoFont;
        DateTime zoomEnd, thumbEnd, pgFlipEnd;
        Point prevFormLocation;
        Size prevFormSize;
        string dir;
        float x, y, prevX, prevY, refX, refY, customX, customY, thumbStripY, thumbControlsY, tessImgFocusX, tessImgFocusY, comicNextY, comicMidpointY;
        float zoom, midpointZoom, nextZoom, customZoom;
        float thumbsOffset, thumbsOffsetMidpoint, tessOffset, nextTessOffset, tessOffsetMidpoint, maxTessOffset;
        int curFile, gifFile, selButton = -1, uploadingInfoDots, comicPageScrolls;
        bool initiated = false, zoomingImg = false, copyImgAfterZoom = false, gif = false, mouseDown = false, mouseOverUI = false, closing = false, slideshow = false, tessMode = false, tessImgFocus = false, comicMode = false, comicScrolling = false, uploading = false, prevFS;

        float starRot, starY;
        int starAlpha, backH, starredListYOffset;
        bool starVisible = false, unstarring;

        List<string> starred;
        int pathLineH, maxPathW;
        List<Tuple<int, int>> tessIndices;
        #endregion


        GraphicsPath genRoundRect(float x, float y, float width, float height)
        {
            float xw = x + width;
            float yh = y + height;
            float xwr = xw - 5;
            float yhr = yh - 5;
            float xr = x + 5;
            float yr = y + 5;
            float r2 = 5 * 2;
            float xwr2 = xw - r2;
            float yhr2 = yh - r2;

            GraphicsPath p = new GraphicsPath();
            p.StartFigure();
            p.AddArc(x, y, r2, r2, 180, 90);
            p.AddLine(xr, y, xwr, y);
            p.AddArc(xwr2, y, r2, r2, 270, 90);
            p.AddLine(xw, yr, xw, yhr);
            p.AddArc(xwr2, yhr2, r2, r2, 0, 90);
            p.AddLine(xwr, yh, xr, yh);
            p.AddArc(x, yhr2, r2, r2, 90, 90);
            p.AddLine(x, yhr, x, yr);
            p.CloseFigure();

            return p;
        }

        Image genXButtonBackground()
        {
            //background for the button in the top-right corner
            Image background = new Bitmap(44, 44);
            Graphics gfx = Graphics.FromImage(background);
            gfx.SmoothingMode = SmoothingMode.HighQuality;

            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddLine(0, 0, 44, 0);
            path.AddLine(44, 0, 44, 44);
            path.AddArc(0, -44, 88, 88, 90, 90);
            path.CloseFigure();

            gfx.FillPath(new SolidBrush(Color.FromArgb(115, Color.Black)), path);

            return background;
        }

        Image genXButton()
        {
            //X-button for the top-right corner
            Image xButton = genXButtonBackground();
            Graphics gfx = Graphics.FromImage(xButton);
            gfx.SmoothingMode = SmoothingMode.HighQuality;

            Pen xPen = new Pen(Color.White, 3);
            gfx.DrawLine(xPen, 33, 11, 20, 24);
            gfx.DrawLine(xPen, 20, 11, 33, 24);

            return xButton;
        }

        Image genAltCloseButton(string iconFileName)
        {
            //full screen button for the top-right corner
            Image xpndButton = genXButtonBackground();
            Image icon = Bitmap.FromFile(Application.StartupPath + "\\icons\\" + iconFileName + ".png");

            Graphics gfx = Graphics.FromImage(xpndButton);
            gfx.SmoothingMode = SmoothingMode.HighQuality;
            gfx.DrawImage(icon, 16, 9);

            icon.Dispose();

            return xpndButton;
        }

        Image genPlayButton()
        {
            Image play = new Bitmap(132, 32);
            Image icon = Bitmap.FromFile(Application.StartupPath + "\\icons\\play.png");

            ColorBlend blend = new ColorBlend();
            blend.Positions = new float[] { 0, 0.5f, 1 };
            blend.Colors = new Color[] { Color.FromArgb(100, Color.Gray), Color.FromArgb(192, 107, 107, 107), Color.FromArgb(192, 159, 159, 159) };

            LinearGradientBrush gradientBrush = new LinearGradientBrush(new Rectangle(0, 0, 1, 1), Color.Black, Color.Black, 0, false);
            gradientBrush.InterpolationColors = blend;

            gradientBrush.ResetTransform();
            gradientBrush.ScaleTransform(64, 64);
            gradientBrush.RotateTransform(90);

            Graphics gfx = Graphics.FromImage(play);
            gfx.SmoothingMode = SmoothingMode.HighQuality;

            gfx.FillPie(gradientBrush, new Rectangle(0, 0, 132, 64), 180, 180); //background
            gfx.DrawPie(new Pen(Color.Black, 4), new Rectangle(2, 2, 128, 56), 180, 180); //outline
            gfx.DrawImage(icon, 50, 0); //icon

            gradientBrush.Dispose();
            gfx.Dispose();
            icon.Dispose();

            return play;
        }

        Image genButton(string iconName)
        {
            Image icon = Bitmap.FromFile(Application.StartupPath + "\\icons\\" + iconName + ".png");
            Image button = new Bitmap(icon.Width, icon.Height);

            Graphics gfx = Graphics.FromImage(button);
            gfx.SmoothingMode = SmoothingMode.HighQuality;

            //draw semi-transparent background
            if (iconName == "zoom toggle")
                gfx.FillEllipse(new SolidBrush(Color.FromArgb(100, Color.Gray)), 0, 3, 14, 10);
            else if (!iconName.Contains("zoom"))
                gfx.FillEllipse(new SolidBrush(Color.FromArgb(100, Color.Gray)), 0, 0, (int)(icon.Width * 0.9375), (int)(icon.Height * 0.9375));
            else
                gfx.FillEllipse(new SolidBrush(Color.FromArgb(100, Color.Gray)), 0, 0, 13, 13);

            //draw icon
            gfx.DrawImage(icon, 0, 0);

            return button;
        }

        TextureBrush genCheckeredPattern()
        {
            Image pattern = new Bitmap(16, 16);
            Graphics tmpGfx = Graphics.FromImage(pattern);
            tmpGfx.Clear(Color.White);

            Brush grayBrush = new SolidBrush(Color.FromArgb(204, 204, 204));
            tmpGfx.FillRectangle(grayBrush, 8, 0, 16, 8);
            tmpGfx.FillRectangle(grayBrush, 0, 8, 8, 16);

            return new TextureBrush(pattern);
        }

        void genCheckers()
        {
            if (checkers != null)
                checkers.Dispose();

            if (imgFiles[curFile].hasTransparency)
            {
                checkers = new Bitmap(img.Width, img.Height);
                Graphics checkGfx = Graphics.FromImage(checkers);
                checkGfx.FillRectangle(texture, 0, 0, img.Width, img.Height);
            }
        }

        void loadDir(string path)
        {
            if (Directory.Exists(path))
            {
                string[] dirFiles = Directory.GetFiles(path);

                if (dirFiles.Length > 0)
                    path = dirFiles[0];
            }

            //stop thumb loading
            if (thumbLoader.IsBusy)
                thumbLoader.CancelAsync();

            assignThumbAndGifLoaders();

            //exit tesserax mode
            tessImgFocus = false;
            tessMode = false;

            //dispose prev data
            if (imgFiles != null)
                foreach (ImageFile imgFile in imgFiles)
                    if (imgFile != null)
                    {
                        ImageAnimator.StopAnimate(imgFile.img, new EventHandler(this.OnFrameChanged));
                        imgFile.DisposeImages();
                    }

            //prepare arrays for this dir
            dir = Path.GetDirectoryName(path);
            string[] files = Misc.GetFilesInNaturalOrder(dir, "*.*", SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)).ToArray();

            imgFiles = new List<ImageFile>();
            for (int i = 0; i < files.Length; i++)
                imgFiles.Add(new ImageFile(files[i]));

            for (curFile = 0; curFile < files.Length; curFile++)
                if (files[curFile] == path)
                    break;

            //ready first img
            imgFiles[curFile].Prepare();
            loadImage();

            //begin loading other thumbs
            int nextFile = curFile + 1;
            if (nextFile == files.Length)
                nextFile = curFile - 1;
            if (nextFile != -1)
                if (!thumbLoader.IsBusy)
                    thumbLoader.RunWorkerAsync(nextFile);
        }

        void loadImage()
        {
            bool firstImg = true;

            if (imgFiles[curFile].smallThumb == null)
                return;
            
            if (img != null)
            {
                firstImg = false;

                img.Dispose();

                if (gif) //stop prev gif
                {
                    gif = false;
                    ImageAnimator.StopAnimate(imgFiles[gifFile].img, new EventHandler(this.OnFrameChanged));
                }
            }

            this.Text = imgFiles[curFile].path + " - Tesserax";

            if (imgFiles[curFile].img != null)
            {
                //img already loaded
                copyImgAndFitToScreen();
                initGIF(curFile);
                genCheckers();
            }
            else
            {
                //display enlarged thumbnail before loading image in full
                copyImgAndFitToScreen();

                //load new img in separate thread
                if (imgLoader.IsBusy)
                    imgLoader.CancelAsync();
                else
                    imgLoader.RunWorkerAsync(curFile);
            }

            //reset stuff
            zoomEnd = DateTime.Now.AddSeconds(-5);

            customX = 0;
            customY = 0;
            customZoom = 0;

            if (firstImg)
            {
                zoomingImg = true;
                copyImgAfterZoom = true;

                zoom = 0.1f;
                midpointZoom = (zoom + nextZoom) / 2;

                x = this.ClientRectangle.Width / 2 - zoom * img.Width / 2;
                y = this.ClientRectangle.Height / 2 - zoom * img.Height / 2;

                alignment = Align.Center;
            }
            else if (tessImgFocus)
            {
                zoomingImg = true;
                
                x = tessImgFocusX;
                y = tessImgFocusY;

                zoom = 0.1f;
                midpointZoom = (zoom + nextZoom) / 2;

                alignment = Align.Center;
            }
            else if (comicMode)
            {
                comicbook();
            }
        }

        void initGIF(int indFile)
        {
            gif = imgFiles[indFile].img.GetFrameCount(new FrameDimension(imgFiles[indFile].img.FrameDimensionsList[0])) > 1;

            if (gif)
            {
                gifFile = indFile;
                ImageAnimator.Animate(imgFiles[indFile].img, new EventHandler(this.OnFrameChanged));
            }
        }

        void assignThumbAndGifLoaders()
        {
            thumbLoader = new BackgroundWorker();
            thumbLoader.WorkerSupportsCancellation = true;
            thumbLoader.DoWork += new DoWorkEventHandler(thumbLoader_DoWork);
            thumbLoader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(thumbLoader_RunWorkerCompleted);
        }

        void getNextFile(int step)
        {
            if (step == 0 || imgFiles.Count == 0)
                return;

            //curFile = (curFile + step) % files.Length;
            curFile += step;

            //if |step|==1 -> go to the other end of the file list
            if (step == 1)
                curFile %= imgFiles.Count;
            else if (step == -1 && curFile == -1)
                curFile = imgFiles.Count - 1;
            //if step>1 -> go to the first or last element in the file list
            else
                curFile = Math.Min(Math.Max(curFile, 0), imgFiles.Count - 1);

            thumbsOffset = 30 * step;
            thumbsOffsetMidpoint = thumbsOffset / 2;
            thumbEnd = DateTime.Now;

            loadImage();
            tmrAnimation.Enabled = true;
        }

        void tessModeScroll(int delta)
        {
            nextTessOffset += delta;
            nextTessOffset = Math.Max(Math.Min(nextTessOffset, 0), maxTessOffset); //bound offset so that imgs are always displayed on screen

            tessOffsetMidpoint = (tessOffset + nextTessOffset) / 2;
        }

        void changeZoom()
        {
            float deltaZoom = calcDelta(zoom, midpointZoom, nextZoom);

            //alignment
            switch (alignment)
            {
                case Align.Center: //center img on window
                    float step = deltaZoom / (nextZoom - zoom);
                    float destX, destY;

                    destX = this.ClientRectangle.Width / 2 - imgFiles[curFile].w * nextZoom / 2;
                    destY = this.ClientRectangle.Height / 2 - imgFiles[curFile].h * nextZoom / 2;

                    x += (destX - x) * step;
                    y += (destY - y) * step;
                    break;
                case Align.Relative: //zoom/unzoom relative to the mouse cursor
                    float relativeX = (refX - x) / zoom;
                    float relativeY = (refY - y) / zoom;

                    x -= relativeX * deltaZoom;
                    y -= relativeY * deltaZoom;
                    break;
                case Align.HoldPosition: //keep img on a fixed position
                    x -= deltaZoom * imgFiles[curFile].w / 2;
                    y -= deltaZoom * imgFiles[curFile].h / 2;
                    break;
                case Align.Custom: //return position to previous value
                    step = deltaZoom / (nextZoom - zoom);

                    x += (customX - x) * step;
                    y += (customY - y) * step;
                    break;
            }

            zoom += deltaZoom;
            if (zoom == nextZoom)
            {
                alignment = Align.Relative;
                zoomEnd = DateTime.Now;

                if (closing)
                    Application.Exit();

                if (imgFiles[curFile].img != null && copyImgAfterZoom)
                {
                    //if img is zoomed out then performance is better if the program uses a new, scaled, copy of the img (especially for very large images)
                    //if img is zoomed in then performance is better if the program uses the original image and stretches it via Graphics.DrawImage()
                    if (zoom <= 1)
                        copyCurFileToImg();
                    else if (img.Size != imgFiles[curFile].img.Size)
                    {
                        img.Dispose();
                        img = new Bitmap(imgFiles[curFile].img);
                    }
                }
            }
        }

        void copyImgAndFitToScreen()
        {
            //file not yet prepared for loading (the user probably browsed through files too quickly)
            if (imgFiles[curFile].w == 0)
                return;

            //calc necessary zoom to fit img to screen
            float zoomVert = (float)this.ClientRectangle.Height / imgFiles[curFile].h;
            float zoomHor = (float)this.ClientRectangle.Width / imgFiles[curFile].w;
            float destZoom = Math.Min(zoomVert, zoomHor);

            if (destZoom > 1)
            {
                //if img is smaller than the screen then don't fit it to screen, just draw it without scaling
                x = this.Width / 2 - imgFiles[curFile].w / 2;
                y = this.Height / 2 - imgFiles[curFile].h / 2;

                //zoom = destZoom;
                zoom = 1;
                destZoom = 1;
            }
            else
            {
                x = this.Width / 2 - destZoom * imgFiles[curFile].w / 2;
                y = this.Height / 2 - destZoom * imgFiles[curFile].h / 2;

                //zoom = 1;
                zoom = destZoom;
            }

            nextZoom = zoom;

            int destW = (int)(imgFiles[curFile].w * destZoom);
            int destH = (int)(imgFiles[curFile].h * destZoom);

            //draw scaled img
            if (img != null)
                img.Dispose();

            img = new Bitmap(destW, destH);

            Graphics gfxImg = Graphics.FromImage(img);

            //use original image if loaded; otherwise use large thumb
            if (imgFiles[curFile].img == null)
                gfxImg.DrawImage(imgFiles[curFile].largeThumb, 0, 0, destW, destH);
            else
                gfxImg.DrawImage(imgFiles[curFile].img, 0, 0, destW, destH);

            gfxImg.Dispose();
        }

        void copyCurFileToImg()
        {
            if (img != null)
                img.Dispose();

            if (zoom == 0)
                return;

            img = new Bitmap((int)(imgFiles[curFile].w * zoom), (int)(imgFiles[curFile].h * zoom));

            Graphics gfxImg = Graphics.FromImage(img);
            gfxImg.DrawImage(imgFiles[curFile].img, 0, 0, (int)(imgFiles[curFile].w * zoom), (int)(imgFiles[curFile].h * zoom));

            gfxImg.Dispose();
        }

        void copyCurFileToImg(float customZoom)
        {
            if (img != null)
                img.Dispose();

            if (customZoom == 0)
                return;

            img = new Bitmap((int)(imgFiles[curFile].w * customZoom), (int)(imgFiles[curFile].h * customZoom));
            Graphics gfxImg = Graphics.FromImage(img);

            if (imgFiles[curFile].img != null)
                gfxImg.DrawImage(imgFiles[curFile].img, 0, 0, (int)(imgFiles[curFile].w * customZoom), (int)(imgFiles[curFile].h * customZoom));
            else
                gfxImg.DrawImage(imgFiles[curFile].largeThumb, 0, 0, (int)(imgFiles[curFile].w * customZoom), (int)(imgFiles[curFile].h * customZoom));

            gfxImg.Dispose();
        }

        #region Drawing Functions
        void drawImage(Graphics gfx)
        {
            if (img == null || img.PixelFormat == PixelFormat.Undefined)
                return; //tesserax is probably closing

            //draw checkered pattern behind transparent imgs
            if (checkers != null && checkers.PixelFormat != PixelFormat.Undefined)
            {
                if (imgFiles[curFile].img == null || zoom != nextZoom || zoom > 1)
                    gfx.DrawImage(checkers, x, y, imgFiles[curFile].w * zoom, imgFiles[curFile].h * zoom);
                else
                    gfx.DrawImage(checkers, x, y, img.Width, img.Height);
            }

            //draw image/animation
            if (gif)
            {
                ImageAnimator.UpdateFrames(); //Get the next frame ready for rendering
                gfx.DrawImage(imgFiles[curFile].img, x, y, imgFiles[curFile].w * zoom, imgFiles[curFile].h * zoom);
            }
            else
            {
                if (zoom != nextZoom || zoom > 1)
                    gfx.DrawImage(img, x, y, imgFiles[curFile].w * zoom, imgFiles[curFile].h * zoom);
                else
                    gfx.DrawImage(img, x, y);
            }
        }

        void drawPrevImage(Graphics gfx, int prevFile)
        {
            float pZoom = Math.Min(1, (float)this.ClientRectangle.Width / imgFiles[prevFile].w);
            int px = this.ClientRectangle.Width / 2 - (int)(pZoom * imgFiles[prevFile].w / 2);
            int py;

            if (comicNextY < y)
                py = (int)(y - imgFiles[prevFile].h * pZoom);
            else
                py = (int)(y + imgFiles[curFile].h * pZoom);

            if (imgFiles[prevFile].img == null || imgFiles[prevFile].img.PixelFormat == PixelFormat.Undefined)
                return; //tesserax is probably closing

            //draw checkered pattern behind transparent imgs
            if (checkers != null && checkers.PixelFormat != PixelFormat.Undefined)
            {
                if (imgFiles[prevFile].img == null || pZoom != nextZoom || pZoom > 1)
                    gfx.DrawImage(checkers, px, py, imgFiles[prevFile].w * pZoom, imgFiles[prevFile].h * pZoom);
                else
                    gfx.DrawImage(checkers, px, py, imgFiles[prevFile].img.Width, imgFiles[prevFile].img.Height);
            }

            //draw image (ignore animation)
            gfx.DrawImage(imgFiles[prevFile].img, px, py, imgFiles[prevFile].w * pZoom, imgFiles[prevFile].h * pZoom);
        }

        void drawThumbStrip(Graphics gfx)
        {
            //animate thumbs moving
            if (thumbsOffset != 0)
                thumbsOffset += calcDelta(thumbsOffset, thumbsOffsetMidpoint, 0);

            double thumbEndElapsed = DateTime.Now.Subtract(thumbEnd).TotalSeconds;
            if (!mouseDown && (mouseOverUI || thumbEndElapsed <= 3.5)) //don't draw thumbs if moving img around
            {
                int alpha = 255;
                if (!mouseOverUI && thumbEndElapsed > 3)
                    alpha = (int)((3.5 - thumbEndElapsed) * 2 * 255);

                //draw grey background strip in full screen
                if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.None)
                    gfx.FillRectangle(thumbStripBrush, 0, thumbStripY, this.Width, 45);

                if (thumbStripY > 100)
                {
                    //current thumb and to the right
                    float thX = this.Width / 2 - 15 + thumbsOffset;

                    for (int i = curFile; i < imgFiles.Count; i++)
                        if (thX > this.Width)
                            break;
                        else
                        {
                            drawThumb(i, gfx, thX, alpha);
                            thX += 30;
                        }

                    //to the left
                    thX = this.Width / 2 - 45 + thumbsOffset;

                    for (int i = curFile - 1; i >= 0; i--)
                        if (thX < -15)
                            break;
                        else
                        {
                            drawThumb(i, gfx, thX, alpha);

                            if (i > 0)
                                thX -= 30;
                        }
                }
            }
        }

        void drawThumb(int ind, Graphics gfx, float thX, int alpha)
        {
            if (imgFiles[ind].smallThumb == null)
                return;

            if (imgFiles[ind].zoom == 1)
            {
                //standard display
                if (alpha == 255)
                    gfx.DrawImage(imgFiles[ind].smallThumb, thX, thumbStripY);
                else
                    gfx.DrawImage(imgFiles[ind].smallThumb, new Rectangle((int)thX, (int)thumbStripY, imgFiles[ind].smallThumb.Width, imgFiles[ind].smallThumb.Height), 0, 0, imgFiles[ind].smallThumb.Width, imgFiles[ind].smallThumb.Height, GraphicsUnit.Pixel, calcTransparentAttribs(alpha));
            }
            else
            {
                //enlarging animation?
                imgFiles[ind].zoom = Math.Min(imgFiles[ind].zoom + 0.05f, 1);

                if (alpha == 255)
                    gfx.DrawImage(imgFiles[ind].smallThumb, thX + 15 * (1 - imgFiles[ind].zoom), thumbStripY + 22.5f * (1 - imgFiles[ind].zoom), 30 * imgFiles[ind].zoom, 45 * imgFiles[ind].zoom);
                else
                    gfx.DrawImage(imgFiles[ind].smallThumb, new Rectangle((int)thX, (int)thumbStripY, imgFiles[ind].smallThumb.Width, imgFiles[ind].smallThumb.Height), 0, 0, imgFiles[ind].smallThumb.Width, imgFiles[ind].smallThumb.Height, GraphicsUnit.Pixel, calcTransparentAttribs(alpha));
            }
        }

        void drawUI(Graphics gfx, bool UIVisible)
        {
            if (!mouseDown)
            {
                if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.None)
                {
                    //draw X-button in top-right corner
                    if (selButton != -2)
                        gfx.DrawImage(xButton, this.Width - 44, 0);
                    else
                        drawBrightenedImage(xButton, new Point(this.Width - 44, 0), gfx);
                }
                else
                {
                    //draw expand button in top-right corner
                    if (selButton != -2)
                        gfx.DrawImage(xpndButton, this.Width - 55, 0);
                    else
                        drawBrightenedImage(xpndButton, new Point(this.Width - 55, 0), gfx);
                }
            }

            //draw controls above thumbs
            if (UIVisible)
            {
                string info;
                if (uploading)
                {
                    if (uploadingInfoDots < 18)
                        uploadingInfoDots++;
                    else
                        uploadingInfoDots = 0;

                    info = "".PadRight(uploadingInfoDots, '.') + "Upload in progress" + "".PadRight(uploadingInfoDots, '.');
                }
                else if (selButton == -1)
                {
                    info = Path.GetFileName(imgFiles[curFile].path) + " (" + imgFiles[curFile].w + " x " + imgFiles[curFile].h + ", " + imgFiles[curFile].size + ")"; //display filename, img dimensions & img size

                    //append url if img is uploaded
                    if (imgFiles[curFile].uploadedURL != null)
                        info += " - Uploaded to: " + imgFiles[curFile].uploadedURL;
                }
                else if (selButton == -2)
                {
                    if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.None)
                        info = "Exit Program";
                    else
                        info = "Full Screen";
                }
                else if (selButton < buttons.Length)
                    info = buttons[selButton].Name;
                else
                {
                    //side button
                    if (starred.Count == 0)
                        info = sideButtons[selButton - buttons.Length].Name;
                    else
                        info = sideButtons[selButton - buttons.Length].Name + " All Starred Images"; //indicate that the side button's operation will apply to all starred images
                }

                Size infoSize = TextRenderer.MeasureText(info, infoFont);

                gfx.DrawString(info, infoFont, Brushes.Black, this.Width / 2 - infoSize.Width / 2 + 2, thumbStripY - infoSize.Height - 2); //shadow
                gfx.DrawString(info, infoFont, Brushes.White, this.Width / 2 - infoSize.Width / 2, thumbStripY - infoSize.Height - 4); //text

                //control buttons
                for (int i = 0; i < buttons.Length; i++)
                    buttons[i].Draw(gfx, i == selButton);

                //draw yellow star if current image is starred
                if (curFileStarred())
                    gfx.DrawImage(yellowStarButton, this.Width / 2 + 162, thumbControlsY);

                //display list of starred files
                SolidBrush backBrush = new SolidBrush(Color.FromArgb(75, Color.Black));
                int y = (int)thumbStripY - 6 + starredListYOffset;

                if (starred.Count > 0)
                    gfx.FillRectangle(backBrush, 44, y - backH, maxPathW + 22, backH); //draw background

                for (int i = starred.Count - 1; i >= 0; i--)
                {
                    y -= pathLineH + 4;

                    //is the text too long?
                    string dispTxt = starred[i];

                    if (TextRenderer.MeasureText(dispTxt, infoFont).Width > maxPathW)
                    {
                        //keep reducing by 10 chars until OK
                        while (TextRenderer.MeasureText(dispTxt, infoFont).Width > maxPathW && dispTxt.Length > 1)
                            dispTxt = dispTxt.Substring(0, Math.Max(dispTxt.Length - 10, 1));

                        //then increase by 1 char until not OK
                        while (TextRenderer.MeasureText(dispTxt, infoFont).Width < maxPathW)
                            dispTxt = starred[i].Substring(0, dispTxt.Length + 1);

                        //finally trim the last char that was over the top
                        dispTxt = dispTxt.Substring(0, Math.Max(dispTxt.Length - 1, 1));
                    }

                    gfx.DrawString(dispTxt, infoFont, Brushes.White, 44, y);
                    gfx.DrawImage(miniStar, 46 + maxPathW, y + 3); //draw star icon next to filename
                }

                if (starred.Count > 0)
                {
                    y -= pathLineH + 4;
                    gfx.DrawString("Folder: " + dir, infoFont, Brushes.LightGray, 44, y);
                }

                //side buttons
                for (int i = 0; i < sideButtons.Length; i++)
                    sideButtons[i].Draw(gfx, i == selButton - buttons.Length);
            }

            if (starVisible)
                drawStar(gfx);
        }

        void drawString(Graphics gfx, string s, int xCenter, int yCenter, int alpha)
        {
            SizeF size = gfx.MeasureString(s, zoomDisplayFont);
            float w = size.Width, h = size.Height;

            float x = xCenter - w / 2;
            float y = yCenter - h / 2;

            Brush zoomDisplayBrush = new SolidBrush(Color.FromArgb(alpha, Color.White)), zoomDisplayBackground = new SolidBrush(Color.FromArgb(alpha, 83, 83, 90));

            gfx.FillPath(zoomDisplayBackground, genRoundRect(xCenter - 4, yCenter - 5, w + 4, h + 6));
            gfx.DrawString(s, zoomDisplayFont, zoomDisplayBrush, xCenter, yCenter);

            zoomDisplayBrush.Dispose();
        }

        bool drawZoomOverlay(Graphics gfx)
        {
            double zoomEndElapsed = DateTime.Now.Subtract(zoomEnd).TotalSeconds;
            
            if (zoom != nextZoom || zoomEndElapsed < 1.5) //display zoom value
            {
                int alpha = 255;
                if (zoom == nextZoom && zoomEndElapsed > 1)
                    alpha = (int)((1.5 - zoomEndElapsed) * 255);

                drawString(gfx, ((int)(zoom * 100)).ToString() + "%", this.Width / 2, this.Height / 2, alpha);
            }

            return zoomEndElapsed < 2;
        }

        void drawStar(Graphics gfx)
        {
            gfx.TranslateTransform(this.Width / 2, starY);
            gfx.RotateTransform(starRot);
            gfx.TranslateTransform(-yellowStar.Width / 2, -yellowStar.Width / 2);

            if (!unstarring)
            {
                if (starY > this.Height / 2)
                { 
                    //star rotating and moving upwards
                    gfx.DrawImage(yellowStar, 0, 0);

                    starY -= 25;
                }
                else
                {
                    //star rotating and fading
                    gfx.DrawImage(yellowStar, new Rectangle(0, 0, yellowStar.Width, yellowStar.Height), 0, 0, yellowStar.Width, yellowStar.Height, GraphicsUnit.Pixel, calcTransparentAttribs(starAlpha));

                    starAlpha -= 15;

                    if (starAlpha == 0)
                        starVisible = true;
                }
            }
            else
            {
                //star rotating and moving downwards and fading
                gfx.DrawImage(blackStar, new Rectangle(0, 0, blackStar.Width, blackStar.Height), 0, 0, blackStar.Width, blackStar.Height, GraphicsUnit.Pixel, calcTransparentAttribs(starAlpha));

                starAlpha -= 15;
                starY += 25;

                if (starY > this.ClientRectangle.Height)
                    starVisible = true;
            }

            starRot += 15;

            gfx.ResetTransform();
        }

        static void drawBrightenedImage(Image img, Point location, Graphics gfx)
        {
            ImageAttributes imgAttribs = new ImageAttributes();
            imgAttribs.SetGamma(0.5f);

            gfx.DrawImage(img, new Rectangle(location, img.Size), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imgAttribs);
        }

        static ImageAttributes calcTransparentAttribs(int alpha)
        {
            float[][] matrixItems = { 
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, (float)alpha / 255, 0}, 
                new float[] {0, 0, 0, 0, 1}};

            ColorMatrix colorMatrix = new ColorMatrix(matrixItems);
            ImageAttributes attribs = new ImageAttributes();

            attribs.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            return attribs;
        }

        void drawTesseraxMode(Graphics gfx)
        {
            //change offset when scrolling
            if (tessOffset != nextTessOffset)
                tessOffset += calcDelta(tessOffset, tessOffsetMidpoint, nextTessOffset);

            //draw thumbs
            //tessIndices is the list of thumbs' indices sorted by thumb width
            //the first column of thumbs selects thumbs from the end of tessIndices (widest)
            //the second column selects from the beginning of tessIndices (narrowest)
            //alternating widest/narrowest columns like this matches the most similar (in width) thumbs together
            List<int> nextColumn;
            int tessX = (int)tessOffset, tessY = 0, lb = 0, ub = tessIndices.Count - 1, h, nextThumb, columnInd = 0, columnWidth, ratioDiff;
            bool thumbsVisible = tessX + 256 > 0;

            while (lb <= ub)
            {
                //prepare next column
                nextColumn = new List<int>();
                h = 0;
                columnWidth = 0;

                while (h < this.ClientRectangle.Height && lb <= ub)
                {
                    if (columnInd % 2 == 0)
                        nextThumb = tessIndices[ub--].Item2;
                    else
                        nextThumb = tessIndices[lb++].Item2;

                    nextColumn.Add(nextThumb);

                    if (imgFiles[nextThumb].largeThumb.Width > columnWidth)
                        columnWidth = imgFiles[nextThumb].largeThumb.Width;

                    h += imgFiles[nextThumb].largeThumb.Height;
                }

                tessY -= (h - this.ClientRectangle.Height) / 2; //align column to vertical center

                //draw column
                for (int i = 0; i < nextColumn.Count; i++)
                {
                    nextThumb = nextColumn[i];
                    
                    ratioDiff = imgFiles[nextThumb].largeThumb.Height * (columnWidth - imgFiles[nextThumb].largeThumb.Width) / columnWidth;

                    if (thumbsVisible)
                    {
                        gfx.DrawImage(imgFiles[nextThumb].largeThumb, new Rectangle(tessX, tessY, columnWidth, imgFiles[nextThumb].largeThumb.Height), new Rectangle(0, ratioDiff / 2, imgFiles[nextThumb].largeThumb.Width, imgFiles[nextThumb].largeThumb.Height - ratioDiff), GraphicsUnit.Pixel);

                        //starred?
                        if (fileStarred(nextThumb))
                            gfx.DrawImage(yellowStar, tessX, Math.Max(0, tessY));
                    }

                    tessY += imgFiles[nextThumb].largeThumb.Height;
                }

                //switch to next column
                tessX += columnWidth;
                tessY = 0;
                columnInd++;

                if (!thumbsVisible && tessX + 256 > 0) //column visible -> start drawing thumbs
                    thumbsVisible = true;

                if (tessX > this.ClientRectangle.Width) //no more space
                    break;
            }
        }
        #endregion

        void fullScreenMode()
        {
            //grab screenshot to use as semi-transparent background
            Rectangle area = Screen.GetWorkingArea(this);

            this.Opacity = 0;

            background = new Bitmap(area.Size.Width, area.Size.Height);
            Graphics gfx = Graphics.FromImage(background);
            gfx.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, background.Size, CopyPixelOperation.SourceCopy);

            this.Opacity = 1;

            //draw 1-pixel-wide line on top (to conceal potential smoothing mode errrors)
            gfx.DrawLine(new Pen(Color.Black, 1.0f), 0, 0, area.Width, 0);

            //dim img
            gfx.FillRectangle(new SolidBrush(Color.FromArgb(100, Color.Black)), 0, 0, background.Width, background.Height);

            //change form options
            prevFormLocation = this.Location;
            prevFormSize = this.Size;

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;

            this.Size = area.Size;
            this.Location = area.Location;

            resizeUI();
        }

        void windowMode()
        {
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;

            this.Size = prevFormSize;
            this.Location = prevFormLocation;

            if (background != null)
            {
                background.Dispose();
                background = null;
            }

            resizeUI();
            thumbEnd = DateTime.Now;
        }

        void toggleWindowMode()
        {
            if (slideshow)
                exitSlideshow();
            else if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.Sizable)
                fullScreenMode();
            else
                windowMode();
        }

        void toggleZoomModeOriginal()
        {
            if (zoom != 1.0f)
            {
                customZoom = zoom;
                nextZoom = 1.0f;
            }
            else if (customZoom == 0.0f)
                return;
            else
                nextZoom = customZoom;

            alignment = Align.HoldPosition;
            midpointZoom = (zoom + nextZoom) / 2;

            prepareZoom();

            zoomEnd = DateTime.Now;
            tmrAnimation.Enabled = true;
        }

        void toggleZoomModeFitScreen()
        {
            //calc necessary zoom
            float zoomVert = (float)this.ClientRectangle.Height / imgFiles[curFile].h;
            float zoomHor = (float)this.ClientRectangle.Width / imgFiles[curFile].w;
            float targetZoom = Math.Min(zoomVert, zoomHor);

            if (zoom != targetZoom)
            {
                customZoom = zoom;
                customX = x;
                customY = y;

                nextZoom = targetZoom;
                alignment = Align.Center;
            }
            else
            {
                nextZoom = customZoom;
                alignment = Align.Custom;
            }

            midpointZoom = (zoom + nextZoom) / 2;

            prepareZoom();

            zoomEnd = DateTime.Now;
            tmrAnimation.Enabled = true;
        }

        void fitScreenInstantly(Image imgRef)
        {
            //calc necessary zoom
            float zoomVert = (float)this.ClientRectangle.Height / imgRef.Height;
            float zoomHor = (float)this.ClientRectangle.Width / imgRef.Width;
            zoom = Math.Min(zoomVert, zoomHor);
            nextZoom = zoom;

            x = this.Width / 2 - zoom * imgRef.Width / 2;
            y = this.Height / 2 - zoom * imgRef.Height / 2;
        }

        void resizeUI()
        {
            if (img != null)
            {
                x = this.ClientRectangle.Width / 2 - img.Width / 2;
                y = this.ClientRectangle.Height / 2 - img.Height / 2;
            }

            thumbStripY = this.Height - 45;
            if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.Sizable)
                thumbStripY -= 40;
            thumbControlsY = thumbStripY - TextRenderer.MeasureText("Asdf", infoFont).Height - 36;

            foreach (UIButton button in buttons)
                button.Relocate(this.Width / 2, (int)thumbControlsY);

            foreach (ActionButton button in sideButtons)
                button.Relocate((int)thumbControlsY);

            calcStarredListSize();

            this.Refresh();
        }

        float calcDelta(float value, float midpoint, float target)
        {
            //calculate change in value based on target and midpoint values to create a smooth transition
            float maxDelta = (target - midpoint) / 3;
            float delta = maxDelta * ((float)Math.Cos((value - midpoint) / Math.Abs(target - midpoint) * Math.PI / 3));

            if (Math.Abs(target - value) < Math.Abs(delta)) //don't overshoot the target
                delta = target - value;

            return delta;
        }

        void calcMaxTessOffset()
        {
            //first get list of all loaded thumbs
            tessIndices = new List<Tuple<int, int>>();

            for (int i = 0; i < imgFiles.Count; i++)
                if (imgFiles[i].largeThumb != null)
                    tessIndices.Add(new Tuple<int, int>(imgFiles[i].largeThumb.Width, i));

            //and sort them by width
            tessIndices = tessIndices.OrderBy(ti => ti.Item1).ToList();

            //then make a pass similar to drawTesserax, but without any drawing (to calculate width of all the thumbs put together)
            List<int> nextColumn;
            int tessX = (int)tessOffset, lb = 0, ub = tessIndices.Count - 1, h, nextThumb, columnInd = 0, columnWidth;

            while (lb <= ub)
            {
                //prepare next column
                nextColumn = new List<int>();
                h = 0;
                columnWidth = 0;

                while (h < this.ClientRectangle.Height && lb <= ub)
                {
                    if (columnInd % 2 == 0)
                        nextThumb = tessIndices[ub--].Item2;
                    else
                        nextThumb = tessIndices[lb++].Item2;

                    nextColumn.Add(nextThumb);

                    if (imgFiles[nextThumb].largeThumb.Width > columnWidth)
                        columnWidth = imgFiles[nextThumb].largeThumb.Width;

                    h += imgFiles[nextThumb].largeThumb.Height;
                }

                //switch to next column
                tessX += columnWidth;
                columnInd++;
            }

            maxTessOffset = -(tessX + 256 - this.ClientRectangle.Width);
        }

        void exit()
        {
            if (tessImgFocus)
                tessImgFocus = false;
            else if (tessMode)
                tessMode = false;
            else if (comicMode)
                comicMode = false;
            else if (slideshow)
                exitSlideshow();
            else
            {
                alignment = Align.HoldPosition;
                nextZoom = 0;
                midpointZoom = zoom / 2;

                closing = true;
                zoomEnd = DateTime.Now;
                tmrAnimation.Enabled = true;
            }
        }

        void exitSlideshow()
        {
            timerSlideshow.Enabled = false;
            slideshow = false;

            if (prevFS)
            {
                this.Size = Screen.GetWorkingArea(this).Size;
            }
            else
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;

                this.Size = prevFormSize;
                this.Location = prevFormLocation;
            }
        }

        #region UIButton Events
        void play()
        {
            prevFS = this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.None;
            if (!prevFS)
            {
                prevFormLocation = this.Location;
                prevFormSize = this.Size;
            }

            Rectangle area = Screen.GetBounds(this);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Size = area.Size;
            this.Location = area.Location;

            fitScreenInstantly(imgFiles[curFile].img);

            slideshow = true;
            timerSlideshow.Enabled = true;
            this.Invalidate();
        }

        void prev()
        {
            getNextFile(-1);
        }

        void next()
        {
            getNextFile(1);
        }

        void zoomIn()
        {
            refX = this.Width / 2;
            refY = this.Height / 2;

            nextZoom += 0.3f / (1.0f / zoom);
            midpointZoom = (zoom + nextZoom) / 2;

            if ((nextZoom >= 1 && zoom < 1) || nextZoom < 1)
                copyImgAfterZoom = true;
            else
                copyImgAfterZoom = false;

            zoomEnd = DateTime.Now;
            tmrAnimation.Enabled = true;
        }

        void zoomOut()
        {
            refX = this.Width / 2;
            refY = this.Height / 2;

            nextZoom = Math.Max(0.1f, nextZoom - 0.3f / (1.0f / zoom));
            midpointZoom = (zoom + nextZoom) / 2;

            copyImgAfterZoom = false;

            if (nextZoom < 1) //to improve performance while zooming out it's best to copy the resulting img (at nextZoom) now
                copyCurFileToImg(nextZoom);

            zoomEnd = DateTime.Now;
            tmrAnimation.Enabled = true;
        }

        void zoomToggle()
        {
            if (zoom != 1 || customZoom != 0)
                toggleZoomModeOriginal();
            else
                toggleZoomModeFitScreen();
        }

        void rotateCCW()
        {
            img.RotateFlip(RotateFlipType.Rotate270FlipNone);
        }

        void rotateCW()
        {
            img.RotateFlip(RotateFlipType.Rotate90FlipNone);
        }

        void star()
        {
            starImage(curFile);
        }

        void starImage(int ind)
        {
            starAlpha = 255;

            if (!fileStarred(ind))
            {
                starY = this.ClientRectangle.Height;
                unstarring = false;
                starVisible = true;

                starred.Add(Path.GetFileName(imgFiles[ind].path));
            }
            else
            {
                starY = this.ClientRectangle.Height / 2;
                unstarring = true;
                starVisible = true;

                starred.Remove(Path.GetFileName(imgFiles[ind].path));
            }

            calcStarredListSize();
        }

        void starAll()
        {
            foreach (ImageFile imgFile in imgFiles)
                starred.Add(Path.GetFileName(imgFile.path));

            starAlpha = 255;
            starY = this.ClientRectangle.Height;
            unstarring = false;
            starVisible = true;

            calcStarredListSize();
        }

        void starNone()
        {
            starred.Clear();

            starAlpha = 255;
            starY = this.ClientRectangle.Height / 2;
            unstarring = true;
            starVisible = true;

            calcStarredListSize();
        }

        void locateOnDisk()
        {
            Process.Start("explorer.exe", " /select, " + imgFiles[curFile].path);
        }

        void tesserax()
        {
            tessOffset = 0;
            tessOffsetMidpoint = 0;
            nextTessOffset = 0;
            calcMaxTessOffset();

            tessMode = true;
        }

        void comicbook()
        {
            pgFlipEnd = DateTime.Now;

            Rectangle area = Screen.GetBounds(this);

            if (this.Size != area.Size)
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.Size = area.Size;
                this.Location = area.Location;
            }

            copyCurFileToImg(calcFitComicPageZoom()); //resize img to fit page width to screen

            alignment = Align.Center;
            x = this.ClientRectangle.Width / 2 - img.Width / 2;

            if (img.Height > this.ClientRectangle.Height)
                y = 0;
            else
                y = this.ClientRectangle.Height / 2 - img.Height / 2;

            zoom = 1;
            nextZoom = 1;
            comicNextY = y;

            //determine background mode by averaging RGB values of all the pixels on the very edge of the comic book page
            Bitmap thumb = new Bitmap(imgFiles[curFile].largeThumb);
            double r = 0, g = 0, b = 0;

            //top & bottom edge
            for (int i = 0; i < thumb.Width; i++)
            {
                addPixelRGB(thumb, i, 0, ref r, ref g, ref b);
                addPixelRGB(thumb, i, thumb.Height - 1, ref r, ref g, ref b);
            }

            //left & right edge
            for (int i = 0; i < thumb.Height; i++)
            {
                addPixelRGB(thumb, 0, i, ref r, ref g, ref b);
                addPixelRGB(thumb, thumb.Width - 1, i, ref r, ref g, ref b);
            }

            //average RGB values
            int n = 2 * thumb.Width + 2 * thumb.Height;
            comicBackground = Color.FromArgb((int)(r / n), (int)(g / n), (int)(b / n));

            comicPageScrolls = 0;
            comicMode = true;
        }
        #endregion

        #region SideButton Events
        void copy()
        {
            StringCollection files = new StringCollection();

            if (starred.Count == 0)
                files.Add(imgFiles[curFile].path);
            else
                files.AddRange(starred.Select(s => dir + "\\" + s).ToArray());

            Clipboard.SetFileDropList(files);
        }

        void delete()
        {
            string path;
            bool loadNextImg = false;

            if (starred.Count == 0)
            {
                if (MessageBox.Show("Send this file to the Recycle Bin?", imgFiles[curFile].path, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.OK)
                {
                    //img = this.Icon.ToBitmap();
                    loadNextImg = true;

                    path = imgFiles[curFile].path;

                    imgFiles[curFile].DisposeImages();
                    imgFiles.RemoveAt(curFile);
                    DelToBin.Send(path);
                }
            }
            else if (starred.Count == 1)
            {
                if (MessageBox.Show("Send this file to the Recycle Bin?", dir + "\\" + starred[0], MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.OK)
                {
                    int ind = getIndexOfStarredItem(0);

                    if (ind == curFile)
                        //img = this.Icon.ToBitmap();
                        loadNextImg = true;

                    path = dir + "\\" + starred[0];

                    imgFiles[ind].DisposeImages();
                    imgFiles.RemoveAt(ind);
                    DelToBin.Send(path);

                    starNone();
                }
            }
            else
            {
                if (MessageBox.Show("Send all starred files to the Recycle Bin?", starred.Count + " starred images will be deleted", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.OK)
                {
                    int ind;

                    for (int i = 0; i < starred.Count; i++)
                    {
                        ind = getIndexOfStarredItem(i);

                        if (ind == curFile)
                            //img = this.Icon.ToBitmap();
                            loadNextImg = true;
                        else if (ind < curFile)
                            curFile--; //an image before the currently viewed one will be deleted, so update the current image's index

                        path = dir + "\\" + starred[i];

                        imgFiles[ind].DisposeImages();
                        imgFiles.RemoveAt(ind);

                        DelToBin.Send(path);
                    }

                    starNone();
                }
            }

            if (loadNextImg)
            {
                curFile = Math.Min(curFile, imgFiles.Count - 1);

                if (curFile == -1)
                    img = this.Icon.ToBitmap();
                else
                    loadImage();
            }
        }

        void edit()
        {
            if (starred.Count == 0)
                //Process.Start("CMD.exe" "c:\\windows\\system32\\mspaint.exe );
                Process.Start("c:\\windows\\system32\\mspaint.exe", "\"" + imgFiles[curFile].path + "\"");
            else
                foreach (string starredImg in starred)
                    Process.Start("c:\\windows\\system32\\mspaint.exe", "\"" + dir + "\\" + starredImg + "\"");
        }

        void pixlr()
        {
            onlineService(Services.Pixlr);
        }

        void upload()
        {
            //first check if already uploaded
            if (starred.Count == 0)
            {
                if (imgFiles[curFile].uploadedURL != null)
                {
                    Process.Start(imgFiles[curFile].uploadedURL);
                    return;
                }
            }

            if (!uploadForm.Visible)
            {
                uploadForm = new Upload();
                uploadForm.returnLinks = returnLinks;

                //create copies of the files (because the originals may be in use by Tesserax which disables FileStream)
                purgeTempDir();

                if (starred.Count > 0)
                    foreach (string starredImg in starred)
                        copyToTempDir(dir + "\\" + starredImg);
                else
                    copyToTempDir(imgFiles[curFile].path);

                uploadForm.Show();
            }
        }

        void google()
        {
            onlineService(Services.Google);
        }

        void karma()
        {
            onlineService(Services.Karma);
        }

        void reddit()
        {
            //first check if already uploaded
            if (starred.Count == 0)
            {
                if (imgFiles[curFile].uploadedURL != null)
                {
                    Services.Reddit(imgFiles[curFile].uploadedURL, Path.GetFileName(imgFiles[curFile].path));
                    return;
                }
            }

            if (!uploadForm.Visible)
            {
                uploadForm = new Upload();
                uploadForm.returnLinks = returnLinks;

                //create copies of the files (because the originals may be in use by Tesserax which disables FileStream)
                purgeTempDir();

                if (starred.Count > 0)
                    foreach (string starredImg in starred)
                        copyToTempDir(dir + "\\" + starredImg);
                else
                    copyToTempDir(imgFiles[curFile].path);

                uploadForm.uploadToReddit = true;
                uploadForm.Show();
            }
        }
        #endregion

        void comicScrollUp()
        {
            if (comicScrolling)
                return;

            if ((img.Height <= this.ClientRectangle.Height || comicNextY == 0) && curFile > 0) //go to prev page
            {
                comicPageScrolls++;
                if (comicPageScrolls == 2)
                {
                    getNextFile(-1);

                    if (img.Height > this.ClientRectangle.Height)
                    {
                        y = this.ClientRectangle.Height - img.Height;
                        comicNextY = y;
                    }

                    comicScrolling = true;

                    y = -img.Height;
                    if (img.Height <= this.ClientRectangle.Height)
                        comicNextY = this.ClientRectangle.Height / 2 - img.Height / 2;
                    else
                        comicNextY = this.ClientRectangle.Height - img.Height;
                    comicMidpointY = (y + comicNextY) / 2;
                }
            }
            else
            {
                comicNextY += 120;
                comicNextY = Math.Min(0, comicNextY);

                comicMidpointY = (y + comicNextY) / 2;
            }
        }

        void comicScrollDown()
        {
            if (comicScrolling)
                return;

            if ((img.Height <= this.ClientRectangle.Height || comicNextY == this.ClientRectangle.Height - img.Height) && curFile < imgFiles.Count - 1) //go to prev page
            {
                comicPageScrolls++;
                if (comicPageScrolls == 2)
                {
                    getNextFile(1);

                    comicScrolling = true;

                    y = this.ClientRectangle.Height;
                    if (img.Height <= this.ClientRectangle.Height)
                        comicNextY = this.ClientRectangle.Height / 2 - img.Height / 2;
                    else
                        comicNextY = 0;
                    comicMidpointY = (y + comicNextY) / 2;
                }
            }
            else
            {
                comicNextY -= 120;
                comicNextY = Math.Max(this.ClientRectangle.Height - img.Height, comicNextY);

                comicMidpointY = (y + comicNextY) / 2;
            }
        }
        
        void calcStarredListSize()
        {
            //calc max width of displayed filenames
            maxPathW = TextRenderer.MeasureText("Folder: " + dir, infoFont).Width;

            foreach (string txt in starred)
            {
                int w = TextRenderer.MeasureText(txt, infoFont).Width;
                if (w > maxPathW)
                    maxPathW = w;
            }

            //don't allow list width to be too big (tesserax mode button is the limit)
            maxPathW = Math.Min(maxPathW, this.Width / 2 - 264 - 44 - 22 - 6);

            //calc height of displayed filenames block
            backH = (pathLineH + 4) * (1 + starred.Count);

            starredListYOffset = 0;
        }

        bool curFileStarred()
        {
            return fileStarred(curFile);
        }

        bool fileStarred(int ind)
        {
            return starred.Contains(Path.GetFileName(imgFiles[ind].path));
        }

        int getIndexOfStarredItem(int ind)
        {
            string path = dir + "\\" + starred[ind];

            for (int i = 0; i < imgFiles.Count; i++)
                if (imgFiles[i].path == path)
                    return i;

            return -1;
        }

        int getIndexOfThumbClicked(Point pos)
        {
            //make a pass similar to drawTesserax, but without any drawing (to determine on what thumb the user has clicked)
            List<int> nextColumn;
            int tessX = (int)tessOffset, lb = 0, ub = tessIndices.Count - 1, h, nextThumb, columnInd = 0, columnWidth;

            while (lb <= ub)
            {
                //prepare next column
                nextColumn = new List<int>();
                h = 0;
                columnWidth = 0;

                while (h < this.ClientRectangle.Height && lb <= ub)
                {
                    if (columnInd % 2 == 0)
                        nextThumb = tessIndices[ub--].Item2;
                    else
                        nextThumb = tessIndices[lb++].Item2;

                    nextColumn.Add(nextThumb);

                    if (imgFiles[nextThumb].largeThumb.Width > columnWidth)
                        columnWidth = imgFiles[nextThumb].largeThumb.Width;

                    h += imgFiles[nextThumb].largeThumb.Height;
                }

                //clicked in this column?
                if (pos.X < tessX + columnWidth)
                {
                    int tessY = -(h - this.ClientRectangle.Height) / 2; //align column to vertical center
                    int thumbH;

                    for (int i = 0; i < nextColumn.Count; i++)
                    {
                        thumbH = imgFiles[nextColumn[i]].largeThumb.Height;

                        if (pos.Y > tessY && pos.Y < tessY + thumbH)
                        {
                            //save center coordinates of thumb
                            tessImgFocusX = tessX + columnWidth / 2;
                            tessImgFocusY = tessY + thumbH / 2;

                            return nextColumn[i];
                        }

                        tessY += thumbH;
                    }

                    //nothing found
                    return -1;
                }

                //switch to next column
                tessX += columnWidth;
                columnInd++;
            }

            //nothing found
            return -1;
        }

        void purgeTempDir()
        {
            if (!Directory.Exists(Application.StartupPath + "\\temp"))
                Directory.CreateDirectory(Application.StartupPath + "\\temp");
            else
                foreach (string file in Directory.GetFiles(Application.StartupPath + "\\temp"))
                {
                    File.SetAttributes(file, FileAttributes.Normal); //in case the file is readonly
                    File.Delete(file);
                }
        }

        string copyToTempDir(string srcPath)
        {
            string destPath = Application.StartupPath + "\\temp\\" + Path.GetFileName(srcPath);
            File.Copy(srcPath, destPath);

            return destPath;
        }

        void returnLinks(string[] links)
        {
            //match uploaded files' filenames with imgFiles to get indices
            string[] files = Misc.GetFilesInNaturalOrder(Application.StartupPath + "\\temp\\");
            string filename;

            for (int i = 0; i < links.Length; i++)
            {
                filename = Path.GetFileName(files[i]);

                foreach (ImageFile imgFile in imgFiles)
                    if (Path.GetFileName(imgFile.path) == filename)
                    {
                        imgFile.uploadedURL = links[i];
                        break;
                    }
            }
        }

        void onlineService(Action<string[], Action<object, RunWorkerCompletedEventArgs>> service)
        {
            string[] links;

            purgeTempDir();

            if (starred.Count == 0)
            {
                links = new string[1];
                links[0] = copyToTempDir(imgFiles[curFile].path);

                if (imgFiles[curFile].uploadedURL != null)
                    links[0] = imgFiles[curFile].uploadedURL;
            }
            else
            {
                links = new string[starred.Count];
                int ind;

                for (int i = 0; i < starred.Count; i++)
                {
                    ind = getIndexOfStarredItem(i);
                    links[i] = copyToTempDir(imgFiles[ind].path);

                    if (imgFiles[ind].uploadedURL != null)
                        links[i] = imgFiles[ind].uploadedURL;
                }
            }

            uploadingInProgress();
            service(links, service_RunWorkerCompleted);
        }

        void uploadingInProgress()
        {
            uploading = true;
            uploadingInfoDots = 0;
        }

        private void service_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            uploading = false;

            string[] links = (string[])e.Result;

            //save urls of uploaded images
            returnLinks(links);
        }

        Size calcImgSize()
        {
            Size imgSize = img.Size;

            if (zoom > 1)
            {
                imgSize.Width = (int)(imgSize.Width * zoom);
                imgSize.Height = (int)(imgSize.Height * zoom);
            }

            return imgSize;
        }

        void prepareZoom()
        {
            if (nextZoom > zoom) //zooming in
            {
                if ((nextZoom >= 1 && zoom < 1) || nextZoom < 1)
                    copyImgAfterZoom = true;
                else
                    copyImgAfterZoom = false;
            }
            else //zooming out
            {
                copyImgAfterZoom = false;

                if (nextZoom < 1) //to improve performance while zooming out it's best to copy the resulting img (at nextZoom) now
                    copyCurFileToImg(nextZoom);
            }
        }

        float calcFitComicPageZoom()
        {
            return Math.Min(1, (float)this.ClientRectangle.Width / imgFiles[curFile].w);
        }

        void addPixelRGB(Bitmap bmp, int x, int y, ref double r, ref double g, ref double b)
        {
            Color pixel = bmp.GetPixel(x, y);

            r += pixel.R;
            g += pixel.G;
            b += pixel.B;
        }

        int nextUnloadedGif()
        {
            for (int i = 0; i < imgFiles.Count; i++)
                if (imgFiles[i].img == null && Path.GetExtension(imgFiles[i].path) == ".gif")
                    return i;

            return -1; //none found
        }


        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            uploadForm = new Upload();

            //init form options
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);

            this.MouseWheel += new MouseEventHandler(Main_MouseWheel);

            this.DragEnter += Main_DragEnter;
            this.DragDrop += Main_DragDrop;

            //init fonts
            zoomDisplayFont = new Font(FontFamily.GenericSansSerif, 13);

            infoFont = new Font("Candara", 12, FontStyle.Bold);
            if (infoFont.Name != "Candara")
                infoFont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold);

            //init star-related stuff
            starred = new List<string>();

            pathLineH = TextRenderer.MeasureText("Asdf", infoFont).Height;

            //generate other UI images & brushes
            texture = genCheckeredPattern();
            xButton = genXButton();
            xpndButton = genAltCloseButton("expand");
            exitTessModeButton = genAltCloseButton("contract");

            yellowStar = Bitmap.FromFile(Application.StartupPath + "\\icons\\yellow star.png");
            yellowStarButton = Bitmap.FromFile(Application.StartupPath + "\\icons\\yellow star button.png");
            blackStar = Bitmap.FromFile(Application.StartupPath + "\\icons\\black star.png");
            miniStar = Bitmap.FromFile(Application.StartupPath + "\\icons\\mini star.png");

            thumbStripBrush = new SolidBrush(Color.FromArgb(38, 39, 37));

            //init UI buttons
            buttons = new UIButton[14];
            int i = 0;

            buttons[i++] = new UIButton("Tesserax Mode", genButton("tesserax"), -264, tesserax);
            buttons[i++] = new UIButton("Locate on Disk", genButton("locate"), -214, locateOnDisk);
            buttons[i++] = new UIButton("Zoom In", genButton("zoom"), -170, zoomIn);
            buttons[i++] = new UIButton("Zoom Out", genButton("unzoom"), -150, zoomOut);
            buttons[i++] = new UIButton("Toggle Zoom", genButton("zoom toggle"), -130, zoomToggle);
            buttons[i++] = new UIButton("Previous Picture", genButton("prev"), -102, prev);
            buttons[i++] = new UIButton("Play Slideshow", genPlayButton(), -66, play);
            buttons[i++] = new UIButton("Next Picture", genButton("next"), 70, next);
            buttons[i++] = new UIButton("Rotate Counter-Clockwise", genButton("rotate ccw"), 114, rotateCCW);
            buttons[i++] = new UIButton("Rotate Clockwise", genButton("rotate cw"), 134, rotateCW);
            buttons[i++] = new UIButton("Star", genButton("star"), 162, star);
            buttons[i++] = new UIButton("Star All", genButton("star all"), 198, starAll);
            buttons[i++] = new UIButton("Star None", genButton("star none"), 234, starNone);
            buttons[i++] = new UIButton("Comic Book Mode", genButton("comicbook"), 284, comicbook);

            sideButtons = new ActionButton[8];
            i = 0;
            sideButtons[i++] = new ActionButton("Copy", genButton("copy"), -(i - 1) * 40, copy);
            sideButtons[i++] = new ActionButton("Delete", genButton("delete"), -(i - 1) * 40, delete);
            sideButtons[i++] = new ActionButton("Edit in Paint", genButton("edit"), -8 - (i - 1) * 40, edit);
            sideButtons[i++] = new ActionButton("Edit in Pixlr", genButton("pixlr"), -8 - (i - 1) * 40, pixlr);
            sideButtons[i++] = new ActionButton("Upload to Imgur", genButton("upload"), -16 - (i - 1) * 40, upload);
            sideButtons[i++] = new ActionButton("Google Search by Image", genButton("google"), -16 - (i - 1) * 40, google);
            sideButtons[i++] = new ActionButton("Search Karma Decay", genButton("karma"), -16 - (i - 1) * 40, karma);
            sideButtons[i++] = new ActionButton("Post to Reddit", genButton("reddit"), -16 - (i - 1) * 40, reddit);

            //prepare background workers
            imgLoader = new BackgroundWorker();
            imgLoader.WorkerSupportsCancellation = true;
            imgLoader.DoWork += new DoWorkEventHandler(imgLoader_DoWork);
            imgLoader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(imgLoader_RunWorkerCompleted);

            assignThumbAndGifLoaders();
        }

        private void Main_Activated(object sender, EventArgs e)
        {
            if (!initiated)
            {
                //FS by default
                fullScreenMode();

                //check arguments for img path
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                    loadDir(args[1]);
                else
                {
                    //debug src img
                    //loadDir(@"C:\pix\art\~amorphisss - The First Snow.jpg");

                    //ensure logo image exists
                    string logoDir = Application.StartupPath + "\\logo";

                    if (!Directory.Exists(logoDir))
                        Directory.CreateDirectory(logoDir);

                    string[] files = Directory.GetFiles(logoDir);

                    if (files.Length == 0 || !files.Contains(logoDir + "\\Tesseract.png"))
                    {
                        //create new image to display
                        Image img = new Bitmap(300, 300);
                        Graphics gfx = Graphics.FromImage(img);

                        gfx.FillRectangle(SystemBrushes.Control, 0, 0, 300, 300);
                        gfx.DrawString("Tesseract v" + VERSION + (VERSION.ToString().Contains('.') ? "" : ".0"), SystemFonts.DefaultFont, SystemBrushes.ControlText, 0, 0);

                        img.Save(logoDir + "\\Tesseract.png", ImageFormat.Png);
                    }

                    //display logo
                    loadDir(logoDir);
                    //loadDir(logoDir + "\\Tesseract.png");
                }

                //begin animation
                thumbEnd = DateTime.Now;
                tmrAnimation.Enabled = true;

                initiated = true;
            }
        }

        private void Main_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Main_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])(e.Data.GetData(DataFormats.FileDrop));

                //accept only first file
                loadDir(paths[0]);
            }
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.Sizable)
                resizeUI();

            if (tessMode)
                calcMaxTessOffset();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //draw background
            if (slideshow)
                e.Graphics.Clear(Color.Black);
            else if (comicMode)
                e.Graphics.Clear(comicBackground);
            else if (background != null)
                e.Graphics.DrawImage(background, 0, 0);
            else
                e.Graphics.Clear(Color.FromArgb(127, 127, 127));

            if (imgFiles != null && imgFiles.Count == 0)
                return;

            if (zoom != nextZoom)
                changeZoom();

            //tesserax mode
            if (tessMode)
            {
                drawTesseraxMode(e.Graphics);

                //draw exit button for tesserax mode in top-right corner
                if (selButton != -2)
                    e.Graphics.DrawImage(exitTessModeButton, this.Width - 44 - (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.Sizable ? 11 : 0), 0);
                else
                    drawBrightenedImage(exitTessModeButton, new Point(this.Width - 44 - (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.Sizable ? 11 : 0), 0), e.Graphics);

                if (tessImgFocus)
                {
                    drawImage(e.Graphics);
                    drawZoomOverlay(e.Graphics);

                    if (starVisible)
                        drawStar(e.Graphics);
                }
                else
                    ImageAnimator.UpdateFrames(); //Get the next frame ready for rendering - in case of any gifs in the mosaic

                return;
            }

            //comicbook mode
            if (comicMode)
            {
                //scroll through pages?
                if (y != comicNextY)
                {
                    y += calcDelta(y, comicMidpointY, comicNextY);

                    if (comicScrolling && y == comicNextY)
                        comicScrolling = false;
                }

                drawImage(e.Graphics);

                if (comicScrolling)
                {
                    //draw prev page too
                    if (comicNextY < y)
                        drawPrevImage(e.Graphics, curFile - 1);
                    else
                        drawPrevImage(e.Graphics, curFile + 1);
                }

                //display current page
                double pgFlipEndElapsed = DateTime.Now.Subtract(pgFlipEnd).TotalSeconds;

                if (pgFlipEndElapsed < 2.5)
                {
                    int alpha = 255;
                    if (pgFlipEndElapsed > 2)
                        alpha = (int)((2.5 - pgFlipEndElapsed) * 255);

                    drawString(e.Graphics, (curFile + 1) + " / " + imgFiles.Count, this.Width - 100, 50, alpha);
                }

                return;
            }

            //draw main img
            drawImage(e.Graphics);

            //don't draw UI if in slideshow mode
            if (slideshow)
                return;

            //draw thumb strip
            bool thumbsVisible = !mouseDown && (mouseOverUI || DateTime.Now.Subtract(thumbEnd).TotalSeconds <= 3.5);
            if (thumbsVisible)
                drawThumbStrip(e.Graphics);

            //draw UI elements
            drawUI(e.Graphics, thumbsVisible);
            bool zoomDispFading = drawZoomOverlay(e.Graphics);

            //disable animation timer if it isn't being used by none for none of these functions::
            if (!zoomDispFading         // * zoom display fade out
                && !thumbsVisible       // * thumb fade out
                && thumbsOffset == 0    // * thumb scrolling
                && !starVisible)        // * star animation
                tmrAnimation.Enabled = false;
            else
                tmrAnimation.Enabled = true;
        }

        private void OnFrameChanged(object o, EventArgs e)
        {
            this.Invalidate(); //Force a call to the Paint event handler. 
        }

        private void Main_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.X <= maxPathW + 26 || e.Y >= thumbControlsY)
                return; //disable dbl clicks over starred list or thumbnail strip

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                toggleWindowMode();
        }

        private void Main_MouseDown(object sender, MouseEventArgs e)
        {
            Size imgSize = calcImgSize();

            if (slideshow)
                exitSlideshow();
            else
                switch (e.Button)
                {
                    case System.Windows.Forms.MouseButtons.Left:
                        if (tessMode && !tessImgFocus)
                        {
                            if (e.X > this.Width - 44 - (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.Sizable ? 11 : 0) && e.Y < 44) //clicked on exit tess mode button -> exit tess mode
                                tessMode = false;
                            else
                            {
                                int thumbInd = getIndexOfThumbClicked(e.Location);

                                if (thumbInd != -1)
                                {
                                    if (Control.ModifierKeys == Keys.Control)
                                        //control pressed -> star or unstar the image
                                        starImage(thumbInd);
                                    else
                                    {
                                        //focus image
                                        tessImgFocus = true;
                                        getNextFile(thumbInd - curFile);
                                    }
                                }
                            }

                            return;
                        }

                        prevX = e.X;
                        prevY = e.Y;

                        mouseDown = true;

                        if (!tessMode && !comicMode)
                        {
                            if (e.X < 40)
                            {
                                mouseDown = false;

                                foreach (ActionButton button in sideButtons)
                                    if (button.ClickIfSelected(e.Location))
                                        break;
                            }
                            else if (starred.Count > 0 && e.X < maxPathW + 26 && e.Y > thumbStripY - 6 - backH + pathLineH) //clicked somewhere on starred list
                            {
                                mouseDown = false;
                                int ind = (int)((e.Y - starredListYOffset - (thumbStripY - 6 - backH + pathLineH)) / (pathLineH + 4)); //index of selected file in starred list

                                if (e.X < maxPathW + 4)
                                    //clicked on filename -> jump to image
                                    getNextFile(getIndexOfStarredItem(ind) - curFile);
                                else
                                {
                                    //clicked on star -> unstar
                                    starred.RemoveAt(ind);
                                    calcStarredListSize();
                                }
                            }
                            else if (e.Y > thumbStripY) //clicked on thumbnail?
                            {
                                mouseDown = false;

                                int diffX;
                                if (e.X > this.Width / 2)
                                    diffX = e.X - (this.Width / 2 - 15);
                                else
                                    diffX = e.X - (this.Width / 2 + 15);

                                int step = diffX / 30;
                                getNextFile(step);
                            }
                            else if (e.Y > thumbControlsY)
                            {
                                if (this.Cursor == Cursors.Hand)
                                    mouseDown = false; //disable dragging & UI hiding if mouse over button

                                foreach (UIButton button in buttons)
                                    if (button.ClickIfSelected(e.Location))
                                        break;
                            }
                            else if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.None)
                            {
                                if (e.X > this.Width - 44 && e.Y < 44) //clicked on X-button -> exit program
                                    exit();
                                else if (e.X < x || e.X > x + imgSize.Width || e.Y < y || e.Y > y + imgSize.Height) //clicked outside of img area -> exit full screen (or if in tesserax focused mode, stop focusing image)
                                {
                                    if (tessImgFocus)
                                        tessImgFocus = false;
                                    else if (comicMode)
                                        comicMode = false;
                                    else
                                        windowMode();
                                }
                            }
                            else if (e.X > this.Width - 44 && e.Y < 44) //clicked on expand button -> go to full screen
                                fullScreenMode();
                        }
                        else if (tessImgFocus && (e.X < x || e.X > x + imgSize.Width || e.Y < y || e.Y > y + imgSize.Height)) //clicked outside of img area -> stop focusing image
                            tessImgFocus = false;
                        break;
                    case System.Windows.Forms.MouseButtons.Middle:
                        if (!comicMode)
                            zoomToggle();
                        break;
                    case System.Windows.Forms.MouseButtons.XButton1: //back buton
                        if (tessMode && !tessImgFocus)
                            tessModeScroll(256);
                        else
                            prev();
                        break;
                    case System.Windows.Forms.MouseButtons.XButton2: //forward button
                        if (tessMode && !tessImgFocus)
                            tessModeScroll(-256);
                        else
                            next();
                        break;
                }
        }

        private void Main_MouseMove(object sender, MouseEventArgs e)
        {
            if (img == null)
                return;

            selButton = -1;

            Size imgSize = calcImgSize();

            if (mouseDown) //dragging
            {
                if (comicMode && y == comicNextY)
                {
                    //limited dragging: only vertical allowed
                    y += e.Y - prevY;
                    if (img.Height > this.ClientRectangle.Height)
                        y = Math.Max(Math.Min(y, 0), this.ClientRectangle.Height - img.Height);
                    else
                        y = Math.Min(Math.Max(y, 0), this.ClientRectangle.Height - img.Height);

                    comicNextY = y;
                    prevY = e.Y;
                }
                else
                {
                    x += e.X - prevX;
                    y += e.Y - prevY;
                    prevX = e.X;
                    prevY = e.Y;
                }

                this.Invalidate();
            }
            else if (e.X < 40)
            {
                if (tessMode || comicMode)
                {
                    if (e.X > x && e.X < x + imgSize.Width) //over img area
                        this.Cursor = Cursors.SizeAll;
                    else
                        this.Cursor = Cursors.Default;
                    return;
                }

                //over side buttons
                for (int i = 0; i < sideButtons.Length; i++)
                    if (sideButtons[i].IsMouseOver(e.Location))
                    {
                        selButton = i + buttons.Length;
                        break;
                    }

                if (selButton != -1)
                    this.Cursor = Cursors.Hand; //over button
                else
                {
                    if (e.X > x && e.X < x + imgSize.Width) //over img area
                        this.Cursor = Cursors.SizeAll;
                    else
                        this.Cursor = Cursors.Default;
                }

                mouseOverUI = true;
                this.Invalidate();
            }
            else if (starred.Count > 0 && e.X < maxPathW + 26 && e.Y > thumbStripY - 6 - backH && !tessMode && !comicMode) //show UI if mouse over starred list
            {
                if (e.Y > thumbStripY - 6 - backH + pathLineH)
                    this.Cursor = Cursors.Hand; //hand cursor over star icon/button
                else
                    this.Cursor = Cursors.Default;

                mouseOverUI = true;
                this.Invalidate();
            }
            else if (e.Y > thumbControlsY)
            {
                if (tessMode || comicMode)
                {
                    if (e.X > x && e.X < x + imgSize.Width) //over img area
                        this.Cursor = Cursors.SizeAll;
                    else
                        this.Cursor = Cursors.Default;
                    return;
                }

                if (e.Y < thumbStripY && !(tessMode || comicMode))
                {
                    //over UI buttons
                    for (int i = 0; i < buttons.Length; i++)
                        if (buttons[i].IsMouseOver(e.Location))
                        {
                            selButton = i;
                            break;
                        }

                    if (selButton != -1)
                        this.Cursor = Cursors.Hand; //over button
                    else
                    {
                        if (e.X > x && e.X < x + imgSize.Width) //over img area
                            this.Cursor = Cursors.SizeAll;
                        else
                            this.Cursor = Cursors.Default;
                    }
                }
                else
                    this.Cursor = Cursors.Hand;

                //over thumbnail strip
                mouseOverUI = true;
                this.Invalidate();
            }
            else
            {
                mouseOverUI = false;
                thumbEnd = DateTime.Now;

                if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.None && (e.X > this.Width - 44 && e.Y < 44)) //...over X-button in full screen
                {
                    this.Cursor = Cursors.Hand;
                    selButton = -2;
                }
                else if (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.Sizable && (e.X > this.Width - 55 && e.Y < 44)) //...over expand button in full screen
                {
                    this.Cursor = Cursors.Hand;
                    selButton = -2;
                }
                else if (tessMode && (e.X > this.Width - 44 - (this.FormBorderStyle == System.Windows.Forms.FormBorderStyle.Sizable ? 11 : 0) && e.Y < 44)) //...over exit tess mode button in full screen
                {
                    this.Cursor = Cursors.Hand;
                    selButton = -2;
                }
                else
                {
                    if (e.X > x && e.X < x + imgSize.Width && e.Y > y && e.Y < y + imgSize.Height && (!tessMode || tessImgFocus)) //...over img area
                        this.Cursor = Cursors.SizeAll;
                    else
                        this.Cursor = Cursors.Default;
                }
            }
        }

        private void Main_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
            this.Invalidate();
        }

        private void Main_MouseWheel(object sender, MouseEventArgs e)
        {
            if (tessMode && !tessImgFocus)
                tessModeScroll(e.Delta);
            else if (comicMode)
            {
                if (e.Delta > 0)
                    comicScrollUp();
                else
                    comicScrollDown();
            }
            else if (starred.Count > 0 && e.X < maxPathW + 26 && e.Y > thumbStripY - 6 - backH)
            {
                //if mouse over starred list -> scroll through them
                starredListYOffset += e.Delta / 6;

                //don't scroll too much in either way
                starredListYOffset = Math.Max(starredListYOffset, 0);
                starredListYOffset = Math.Min(starredListYOffset, backH - ((int)thumbStripY - 12));
            }
            else if (e.Y > thumbStripY)
                //if mouse over thumbnails -> scroll through files
                getNextFile(e.Delta / -120);
            else
            {
                //otherwise -> zoom/unzoom
                if (e.Delta > 0)
                    zoomIn();
                else
                    zoomOut();

                refX = e.X;
                refY = e.Y;
            }
        }

        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.D1:
                    toggleZoomModeOriginal();
                    break;
                case Keys.D2:
                    toggleZoomModeFitScreen();
                    break;
                case Keys.Down:
                    if (comicMode)
                        comicScrollDown();
                    else
                        zoomOut();
                    break;
                case Keys.Up:
                    if (comicMode)
                        comicScrollUp();
                    else
                        zoomIn();
                    break;
                case Keys.Left:
                    if (tessMode && !tessImgFocus)
                        tessModeScroll(256 * (e.Control ? 10 : 1));
                    else if (comicMode)
                        getNextFile(-1);
                    else
                        getNextFile(-1 * (e.Control ? 10 : 1));
                    break;
                case Keys.Right:
                    if (tessMode && !tessImgFocus)
                        tessModeScroll(-256 * (e.Control ? 10 : 1));
                    else if (comicMode)
                        getNextFile(-1);
                    else
                        getNextFile(1 * (e.Control ? 10 : 1));
                    break;
                case Keys.Home:
                    if (tessMode)
                    {
                        nextTessOffset = 0;
                        tessOffsetMidpoint = tessOffset / 2;
                    }
                    else
                        getNextFile(-curFile);
                    break;
                case Keys.End:
                    if (tessMode)
                    {
                        nextTessOffset = maxTessOffset;
                        tessOffsetMidpoint = (tessOffset + nextTessOffset) / 2;
                    }
                    else
                        getNextFile(imgFiles.Count - curFile - 1);
                    break;
                case Keys.Space:
                    if (comicMode)
                    {
                        //scroll down the page a little bit less than 1 full view
                        //only scroll to the next page if the current page's bottom is visible currently
                        if (img.Height <= this.ClientRectangle.Height || comicNextY == this.ClientRectangle.Height - img.Height)
                        {
                            if (curFile < imgFiles.Count - 1)
                            {
                                getNextFile(1);

                                comicScrolling = true;

                                y = this.ClientRectangle.Height;
                                if (img.Height <= this.ClientRectangle.Height)
                                    comicNextY = this.ClientRectangle.Height / 2 - img.Height / 2;
                                else
                                    comicNextY = 0;
                                comicMidpointY = (y + comicNextY) / 2;
                            }
                        }
                        else
                        {
                            comicNextY -= this.ClientRectangle.Height - 100;
                            comicNextY = Math.Max(this.ClientRectangle.Height - img.Height, comicNextY);
                        }
                    }
                    break;
                case Keys.S:
                    if (!tessMode || tessImgFocus)
                        star();
                    break;
                case Keys.F11:
                    toggleWindowMode();
                    break;
                case Keys.L:
                    if (e.Control)
                        toggleWindowMode();
                    break;
                case Keys.Delete:
                    delete();
                    break;
                case Keys.Escape:
                    exit();
                    break;
                case Keys.W:
                    if (e.Control)
                        exit();
                    break;
            }
        }

        private void imgLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            int thumbInd = (int)e.Argument;
            e.Result = new Tuple<int, Image>(thumbInd, ImageFast.FromFile(imgFiles[thumbInd].path));
            e.Cancel = imgLoader.CancellationPending;
        }

        private void imgLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                //call again with current image
                imgLoader.RunWorkerAsync(curFile);
            else
            {
                Tuple<int, Image> result = (Tuple<int, Image>)e.Result;

                if (result.Item1 == curFile)
                {
                    //use loaded image
                    imgFiles[result.Item1].img = result.Item2;

                    float prevY = y, prevComicNextY = comicNextY;

                    if (!zoomingImg) //if zoomingImg is true then tesserax just started, so the first img is zooming in
                        copyImgAndFitToScreen();
                    else
                        zoomingImg = false;

                    if (comicMode)
                    {
                        comicbook();

                        y = prevY;
                        comicNextY = prevComicNextY;
                    }
                    
                    initGIF(result.Item1);
                    
                    //check for transparency
                    Bitmap bmp = new Bitmap(imgFiles[curFile].img);
                    
                    //instead of testing if every single pixel has Alpha values (which is extremely slow) let's try testing a total of 100 evenly spaced pixels
                    int iStep = Math.Max(1, imgFiles[curFile].w / 10);
                    int jStep = Math.Max(1, imgFiles[curFile].h / 10);

                    for (int i = 0; i < imgFiles[curFile].w; i += iStep)
                        for (int j = 0; j < imgFiles[curFile].h; j += jStep)
                            if (bmp.GetPixel(i, j).A != 255)
                            {
                                imgFiles[curFile].hasTransparency = true;
                                break;
                            }

                    bmp.Dispose();

                    genCheckers();
                }
                else
                    //race condition: imgLoader should have returned with e.Cancelled set to true
                    //call again with current image
                    imgLoader.RunWorkerAsync(curFile);
            }
        }

        private void thumbLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            int thumbInd = (int)e.Argument;

            e.Result = imgFiles[thumbInd].path;
            imgFiles[thumbInd].Prepare();
        }

        private void thumbLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                return;

            if (tessMode)
                calcMaxTessOffset();

            if (e.Result.ToString() == imgFiles[curFile].path)
                loadImage();

            //start loading next thumb
            int nextFile;

            if (imgFiles[curFile].smallThumb == null)
                nextFile = curFile;
            else
            {
                nextFile = curFile + 1;
                if (nextFile == imgFiles.Count)
                    nextFile -= 2;
                int diff = 1;

                while (imgFiles[nextFile].smallThumb != null)
                {
                    if (nextFile > curFile)
                    {
                        nextFile = curFile - diff;

                        if (nextFile < 0)
                            nextFile = curFile + ++diff;
                    }
                    else
                    {
                        nextFile = curFile + ++diff;

                        if (nextFile >= imgFiles.Count)
                            nextFile = curFile - diff;
                    }

                    if (nextFile < 0 || nextFile >= imgFiles.Count)
                    {
                        nextFile = -1;
                        break;
                    }
                }
            }

            if (nextFile != -1 && Path.GetDirectoryName(e.Result.ToString()) == dir) //continue loading thumbs (except when dir changes)
                thumbLoader.RunWorkerAsync(nextFile);
        }

        private void tmrAnimation_Tick(object sender, EventArgs e)
        {
            this.Invalidate(); //Force a call to the Paint event handler
        }

        private void timerSlideshow_Tick(object sender, EventArgs e)
        {
            next();
        }
    }
}
