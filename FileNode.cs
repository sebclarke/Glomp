
using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GLib;

namespace glomp {


    public class FileNode : SceneNode {
        
        private readonly int textureWidth = 256;
        private readonly int textureHeight = 128;
        private int textSize = 20;
        
        private Bitmap textLabelBmp = null;
        
        private static readonly float[] exeColour = {0.1f, 0.7f, 0.3f};
        private static readonly float[] fileColour = {0.2f, 0.6f, 0.6f};
        private static readonly float[] roColour = {0.1f, 0.1f, 0.1f};
        private static readonly float[] activeColour = {1.0f, 0.5f, 0.5f};
        private static readonly float[] dirColour = {0.3f, 0.5f, 1.0f};
        private static readonly float[] THUMB_COLOUR = {1.0f, 1.0f, 1.0f};
        private static readonly float ACTIVE_SCALE = 1.3f;
        
        private int displayList;
        private String fileName;
        private String file;
        private int textureIndex;
        private bool isVisible;
        private bool hasTexture;
        private bool isActive;
        private bool isDirectory = false;
        private bool isDimmed = false;
        private bool isDirFaded = false;
        private bool activeTextureLoaded = false;
        private bool isReadOnly = false;
        private bool isExecutable = false;
        private String thumbFileName = "";
        private int thumbTextureIndex;
        private Bitmap thumbBmp;
        private bool isThumbnailed = false;
        private String desc = "";
        private float[] typeColour;
        private float[] currentColour = fileColour;
        private DateTime modifyTime;
        private int numChildren = 0;
        private float dirHeight = 1.0f;
        private float fadeAmount = 0.1f;
        private int numDirs = 0;
        private int numFiles = 0;
        private bool isSelected = false;
        private FileSlice parentSlice;
        
        public bool culled = false;
        

        
        public int NumDirs {
            get { return numDirs; }
            set { numDirs = value; }
        }
        
        public bool Selected {
            get { return isSelected; }
            set {isSelected = value; }
        }
        
        public int NumFiles {
            get { return numFiles; }
            set { numFiles = value; }
        }
        
        
        public bool DirFaded {
            get { return isDirFaded; }
            set { isDirFaded = value; }
        }
        
        public float FadeAmount {
            get { return fadeAmount; }
            set { fadeAmount = value; }
        }
          
        
        public bool Dimmed {
            get { return isDimmed; }
            set { isDimmed = value; }
        }
        
        public String Description {
            get { return desc; }
            set { 
                if(value != null) {
                    desc = value; 
                } else {
                    desc = "";
                }
            }
        }
        
        public float DirHeight {
            get { return dirHeight; }
            set { dirHeight = value; }
        }
        
        public int NumChildren {
            get { return numChildren; }
            set { numChildren = value; }
        }
        
        public DateTime ModifyTime {
            get { return modifyTime; }
            set { modifyTime = value; }
        }
        
        public float[] TypeColour {
            get { return typeColour; }
            set { typeColour = value; }
        }
        
        public bool IsDirectory {
            get { return isDirectory; }
            set { isDirectory = value; }
        }
        
        public bool IsReadOnly {
            get { return isReadOnly; }
            set { isReadOnly = value; }
        }
        
        public String ThumbFileName {
            get { return thumbFileName; }
            set { thumbFileName = value; }
        }
        
        
        public bool IsExecutable {
            get { return isExecutable; }
            set { isExecutable = value; }
        }
        
        public String FileName {
            get { return fileName; }
            set { fileName = value; UpdateBitmap(false); }
        }
        
        public String File {
            get { return file; }
            set { file = value; }
        }
        
        private Vector3 textOffset = new Vector3(-3.1f, 0f, 0f);
        
        public bool Visible {
            get { return isVisible; }
            set { isVisible = value; }
        }
        
        public bool Active {
            get { return isActive; }
            set { isActive = value; }
        }

        public FileNode() 
            : base () {
            hasTexture = isVisible = isActive = false;
            fileName = "";
        }
        
        public FileNode(float _scale)
            : base() {
            scale = _scale;
            hasTexture = isVisible = isActive = false;
            fileName = "";
        }
        
        public FileNode(Vector3 _position)
            : base () {
            position = _position;
            hasTexture = isVisible = isActive = false;
            fileName = "";
        }
        
        public FileNode(Vector3 _position, float _scale) 
            : base () {
            scale = _scale;
            position = _position;
            hasTexture = isVisible = isActive = false;
            fileName = "";
        }
        
        public FileNode(String _fileName)
            : base() {
            hasTexture = isVisible = isActive = false;
            fileName = _fileName;
        }
        
        public void SetDisplayList(int _displayList) {
            displayList = _displayList;
        }
        
        public void GenTexture(bool force) {
            if(hasTexture) {
                if(!force) {
                    return;
                }
            }
            System.Drawing.Imaging.BitmapData data;
            hasTexture = true;
            // initialise for new texture
            textureIndex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureIndex);
            // set up texture filters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            
            UpdateBitmap(false);
            
            // format the texture
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, textLabelBmp.Width, textLabelBmp.Height, 0,
            PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
         
            // upload data to openGL
            data = textLabelBmp.LockBits(new Rectangle(0, 0, textLabelBmp.Width, textLabelBmp.Height), 
                                                        System.Drawing.Imaging.ImageLockMode.ReadOnly, 
                                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, textLabelBmp.Width, textLabelBmp.Height, 0,
                                                        PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0); 
                                                        textLabelBmp.UnlockBits(data);
            textLabelBmp.Dispose();
            
            // right... now do the thumbnail if we have one
            
            if(thumbFileName.Length > 0) {
                
                // set up GL for the texture
                thumbTextureIndex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, thumbTextureIndex);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
                
                // get at the bitmap data for the thumbnail
                thumbBmp = new Bitmap(thumbFileName);
                
                if(true) {
                    Bitmap scaled = new Bitmap(128, 128);
                    int x = 0;
                    int y = 0;
                    if(thumbBmp.Width > thumbBmp.Height) {
                        y = (thumbBmp.Width - thumbBmp.Height)/2;
                        x = 0;
                    } else if(thumbBmp.Width < thumbBmp.Height) {
                        y = 0;
                        x = (thumbBmp.Height - thumbBmp.Width)/2;
                    } else {
                        x = 0;
                        y = 0;
                    }
                    
                    
                    using (Graphics gfx = Graphics.FromImage(scaled)) {
                        gfx.Clear(Color.White);
                        gfx.DrawImageUnscaled(thumbBmp, x, y);
                    }
                    thumbBmp = scaled;
                }
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, thumbBmp.Width, thumbBmp.Height, 0,
                                                                PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
                
                // upload data to openGL
                data = thumbBmp.LockBits(new Rectangle(0, 0, thumbBmp.Width, thumbBmp.Height), 
                                                        System.Drawing.Imaging.ImageLockMode.ReadOnly, 
                                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, thumbBmp.Width, thumbBmp.Height, 0,
                                                        PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0); 
                thumbBmp.UnlockBits(data);
                thumbBmp.Dispose();
                isThumbnailed = true;
            }
     
        }
        
        
        public void DestroyTexture() {
            if(hasTexture) {
                hasTexture = false;
                int[] textures = {textureIndex,thumbTextureIndex};
                GL.DeleteTextures(2, textures);
            }
            //textLabelBmp.Dispose();
        }
        
        public override void Render() {
            // push, render box, pop
            /** NOT USED           
            GL.PushMatrix();
            MoveIntoPosition(false);
            RenderBox();
            
            if(isVisible) {
                RenderLabel();
            } 
            GL.PopMatrix();
            */
           
        }
        
        public void DrawBox(int offset) {
            PreRenderBox();
            
            MoveIntoPosition(true);
            
            if(isDirectory) {
                currentColour = dirColour;
                GL.Translate(0f, dirHeight-1.0f, 0f);
                GL.Scale(1f, dirHeight, 1f);
            } else {
                if(isThumbnailed) {
                    currentColour = THUMB_COLOUR;  
                } else if(isExecutable) {
                    currentColour = exeColour;
                } else if(isReadOnly) {
                    currentColour = roColour;
                } else {
                    if(typeColour != null) {
                        currentColour = typeColour;
                    } else {
                        currentColour = fileColour;
                    }
                } 
            }
            
            if(isDirFaded) {
                GL.Color4(currentColour[0], currentColour[1], currentColour[2], fadeAmount);
            } else {
                GL.Color4(currentColour[0], currentColour[1], currentColour[2], parentSlice.Alpha);
            }
                
            
            if(isActive) {
                if(!IsDirectory) {
                    GL.Scale(ACTIVE_SCALE, ACTIVE_SCALE, ACTIVE_SCALE);
                    GL.Translate(Vector3.UnitY * 0.5f);
                } 
                if(!isThumbnailed) {
                    GL.Color4(activeColour[0], activeColour[1], activeColour[2], parentSlice.Alpha);
                }
            }            
            
            if(parentSlice.IsScaled) {
                float scaleValue = parentSlice.Scale - (offset / (float)parentSlice.NumFiles);
                if(scaleValue > 1.0f) { scaleValue = 1.0f; }
                else if(scaleValue < 0f) { scaleValue = 0f; }
                GL.Scale(scaleValue, scaleValue, scaleValue);
            }
            
            
            if(isSelected) {
                GL.PushAttrib(AttribMask.EnableBit|AttribMask.PolygonBit|AttribMask.CurrentBit);
                GL.Disable(EnableCap.Lighting);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.Disable(EnableCap.Texture2D);
                GL.Color4(Color.White);
                GL.CallList(displayList);
                GL.PopAttrib();
            
                GL.Scale(0.8f, 0.8f, 0.8f);
    
            }
            
            GL.CallList(displayList);
            
            
            PostRenderBox();
                
        }
        
        public void DrawLabel() {
            if(isVisible) {
                PreRenderLabel();
         
                MoveIntoPosition(false);

                if(isActive) {
                    GL.Scale(Vector3.One * 1.3f);
                    if(IsDirectory) {
                        GL.Translate(new Vector3(0.14f, 0.35f, 0.0f));
                    } else {
                        GL.Translate(new Vector3(0.0f, 0.35f, 0.0f));
                    }
                    GL.Disable(EnableCap.DepthTest);
                    GL.Color4(1.0f, 0.4f, 0.4f, parentSlice.Alpha);
                } else {
                    GL.Color4(0.4f, 1f, 0.8f, parentSlice.Alpha);
                }

                GL.Begin(BeginMode.Quads);
                GL.TexCoord2(1.0f, 1.0f); 
                GL.Vertex3(-5.1f, -1.0f,  0.0f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 1.0f); 
                GL.Vertex3( -1.1f, -1.0f,  0.0f);  // Bottom Left Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); 
                GL.Vertex3( -1.1f,  1.0f,  0.0f);  // Top Left Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); 
                GL.Vertex3(-5.1f,  1.0f,  0.0f);  // Top Right Of The Texture and Quad
                GL.End();
                
                if(isActive) {
                    GL.Enable(EnableCap.DepthTest);
                }
                
                PostRenderLabel();
            }
        }
        
     
        private void PreRenderLabel() {
            GL.PushMatrix();
            GL.BindTexture(TextureTarget.Texture2D, textureIndex);
        }
        
        private void PostRenderLabel() {
            GL.PopMatrix();
        }
        
        private void PreRenderBox() {
            GL.PushMatrix();
            if(isThumbnailed) {
                GL.PushAttrib(AttribMask.EnableBit);
                GL.Enable(EnableCap.Texture2D);
                GL.BindTexture(TextureTarget.Texture2D, thumbTextureIndex);
            } else if(isDirFaded) {
                GL.PushAttrib(AttribMask.EnableBit);
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
            }
        }
        
        private void PostRenderBox() {
            GL.PopMatrix();
            if(isThumbnailed || isDirFaded) {
                GL.PopAttrib();
            }
        }
        
        public static void SetTextState(bool dimmed) {
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            if(dimmed) {
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
            } else {
                GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
            }
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Lighting);
            GL.PushAttrib(AttribMask.FogBit);
            GL.Disable(EnableCap.Fog);
        }
        
        public static void UnsetTextState(bool dimmed) {
            GL.PopAttrib();
            GL.Disable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend); 
            
        }
        
        public static void SetBoxState(bool dimmed) {
            if(dimmed) {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
                GL.Disable(EnableCap.CullFace);
                GL.PushAttrib(AttribMask.FogBit);
                GL.Disable(EnableCap.Fog);
            }    
        }
        
        public static void UnsetBoxState(bool dimmed) {
            if(dimmed) {
                GL.PopAttrib();
                GL.Enable(EnableCap.CullFace);
                GL.Disable(EnableCap.Blend);   
            }
        }
        
            
        
        // updates the bitmap we use for our text
        private void UpdateBitmap(bool activeTexture) {
            textLabelBmp = new Bitmap(textureWidth, textureHeight);
            //textSize = 20;
            using (Graphics gfx = Graphics.FromImage(textLabelBmp)) {
                String displayText;
                gfx.Clear(Color.Transparent);
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                RectangleF drawRect;
                if(isDirectory) {
                    drawRect = new RectangleF(0f, 0f, textLabelBmp.Width, textLabelBmp.Height);
                } else {
                    drawRect = new RectangleF(0f, 0f, textLabelBmp.Width, textLabelBmp.Height/2 + 7);
                }
                StringFormat drawFormat = new StringFormat();
                drawFormat.Alignment = StringAlignment.Near;
                drawFormat.LineAlignment = StringAlignment.Center;
              
                // truncate string to fit... max 22 chars..
                if(fileName.Length > 27) {
                    displayText = fileName.Substring(0, 27);
                } else {
                    displayText = fileName;
                }
                // scale text to fit depending on length
                if(displayText.Length > 15) {
                    textSize = (int)Math.Round(20.0f / (displayText.Length / 15.0f), 0); 
                }
                
                Font TextFont = new Font(FontFamily.GenericSansSerif, textSize);    
            
                gfx.DrawString(displayText, TextFont, Brushes.White, drawRect, drawFormat);    
                
                if(!isDirectory) {
                    // we are a file, draw another desriptive label
                    
                    TextFont = new Font(FontFamily.GenericSansSerif, 16);
                    drawRect = new RectangleF(5f, (textLabelBmp.Height/2) -7, textLabelBmp.Width, textLabelBmp.Height);
                    drawFormat.LineAlignment = StringAlignment.Near;
                    
                    gfx.DrawString(desc, TextFont, Brushes.BlueViolet, drawRect, drawFormat);
                }
            }
        }
        
        public void SetParent(FileSlice newParent) {
            parentSlice = newParent;
        }
        
       
        
        
        
    }
}
