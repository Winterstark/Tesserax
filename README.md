Tesserax
========

Tesserax is an image viewer for Windows, designed as an improved version of Google Photo Viewer. It looks very similar to Photo Viewer and shares most of the same controls, but also has some very useful features, such as:
* Support for animated GIFs
* Easy uploading to Imgur, Google Search, Reddit, Karma Decay, etc.
* Starring multiple images and uploading them to the same album
* Tesserax mode - displaying the image contents of a directory in the form of a densely packed mosaic
* Comic book mode


Installation
--------------

1. (You need to have [.NET framework](http://www.microsoft.com/en-us/download/details.aspx?id=30653) installed on your computer)
2. Download [the release](https://github.com/Winterstark/Tesserax/releases)
3. Extract
4. Associate file types with Tesserax:
	1. Right-click on an image file (e.g. JPG)
	2. Select Open with -> Choose default program...
	3. Click Browse... and locate Tesserax.exe


Usage
------

After you associate any file types that you want just run the images and Tesserax will start. Alternatively, you can drag files or folders onto Tesserax to open them.

### User Interface

The hotkeys and controls are basically the same as with Google Photo Viewer:
* Click and drag to move the image around.
* Use the mouse wheel or up/down arrows to zoom in/out.
* Press 1 to toggle between current zoom level and 100%.
* Press 2 to toggle between current zoom level and fit to screen.
* Middle mouse button also toggles between different zoom levels.

To switch between files use:
* Left/right arrows or back/forward mouse buttons switch to the previous/next image in the directory.
* Control + left/right arrow skips 10 images behind/ahead.
* Press the Home or End keys to jump to the first or last image in the directory
* You can also click on a thumnbail to jump to it.
* Move the cursor over the thumbnail strip and scroll with the mouse wheel

Other controls:
* Click outside the image area (or double-click anywhere) to exit fullscreen.
* Click the button in the top-right corner (or double-click anywhere) to return to fullscreen.
* Press Delete to delete the image.
* Escape exits current mode or closes Tesserax.

The buttons above the thumbnail are, from left to right:
* Switch to Tesserax mode
* Locate image on disk
* Zoom in
* Zoom out
* Previous picture
* Start slideshow
* Next picture
* Rotate counter-clockwise
* Rotate clockwise
* Star (current image)
* Star all (all images in current directory)
* Star none
* Switch to Comic book mode
* About Tesserax

The buttons on the left side are, from bottom to top:
* Copy image
* Delete image
* Edit in paint
* Edit online in Pixlr
* Upload to Imgur
* Search Google with image
* Search Karma Decay (Reddit) with image
* Post image to Reddit

Note that these buttons perform their operations on the currently viewed image *or*, if there are any starred images, on *all* of them instead. This makes it easy to upload an album of several images at once.
 
### Starring Images

By starring images you can take a selection of images from the current directory and perform an operation (the left-side buttons) on all of them at once. To star the currently viewed image click the Star button or press S.

You can also use the buttons to star all (or unstar all) images in the directory.

Starred images are displayed in a list on the left side of the screen. You can click on them in the list to unstar them.

While in Tesserax mode, Ctrl+Click an image thumbnail to toggle its starriness. Yup, that's a word.

### Tesserax Mode

Tesserax mode covers the screen with medium-sized thumbnails, organizing them in such a way as to minimize the gaps between them, like a mosaic. Because the thumbnails will not be sorted by the order they appear in the directory, this mode is more suitable for browsing large quantities of images.

To move the mosaic left or right scroll with the mouse wheel or press the left/right arrows (hold Ctrl for larger steps).

Click on a thumbnail to view its image in full size. Ctrl+Click a thumbnail to star it.

Note: when you enter Tesserax mode with a large directory you might see the thumbnails quickly scrolling past each other - this means the thumnails are still loading.

### Comic Book Mode

This feature is useful when you have a comic book where every page is a separate image file. In comic book mode the image is displayed as large as possible, but not wider than the screen and not larger than the original. If there are any gaps to the sides of the page they will be colored with the border color of the page.

You cannot move the page sideways, only up or down: either by scrolling the mouse wheel, clicking and dragging, or with the up and down arrow keys. The left and right keys move to the previous/next page.

If you scroll to the edge of the page a slight delay will happen before the actual page turns. This gives you a little hint to know you've reached the end of the page.


Credits
---------

* Uses [Imgur API](https://api.imgur.com)
* Uses [Microsoft Windows API Code Pack](http://archive.msdn.microsoft.com/WindowsAPICodePack)
* Uses [ImageFast](http://weblogs.asp.net/justin_rogers/articles/131704.aspx) library to speed up image loading
* Tesseract icon from [Wikimedia Commons](http://commons.wikimedia.org/wiki/File:Hypercube.png)
* UI icons by [Adam Whitcroft](http://adamwhitcroft.com/batch/) and [Modern UI Icons](http://modernuiicons.com/)
* Uploading animation by [Load Info](http://www.loadinfo.net)