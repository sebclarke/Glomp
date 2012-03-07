using System;
using System.Drawing;
using GLib;
using System.Collections.Generic;
using Gtk;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using System.Diagnostics;
using System.IO;
using Mono.Unix.Native;
using glomp;


public partial class MainWindow : Gtk.Window {
   
    private Stopwatch frameTimer = new Stopwatch();
    private int frameCounter = 0;
    private long currentTicks = 0;
    
    private float[] lightAmbient = { 0.1f, 0.1f, 0.1f, 1.0f };
    private float[] lightDiffuse = { 1.0f, 1.0f, 1.0f, 1.0f };
    private float[] lightPosition = {50.0f, 100.0f, -20.0f, 1.0f };
    

    private static readonly String START_PATH = "/";
    private static readonly Vector3 CAM_OFFSET = new Vector3(5.0f, -12.0f, 32.0f);
    private static readonly String[] ALPHABET = {"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", 
        "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"};
    //private static readonly float[] HIGH_COLOUR = {0.5f, 0.5f, 0.7f, 0f};
    private static readonly float[] HIGH_COLOUR = {0.11f, 0.15f, 0.25f, 0f};
    private static readonly float[] LOW_COLOUR = {0.06f, 0.06f, 0.13f, 0f};
    private static readonly float[] BLACK = {0f, 0f, 0f, 0f};
    private static readonly float[] BACKGROUND =  { 0.1f, 0.1f, 0.2f, 0.0f };
    
    private static readonly int START_WIDTH = 300;
    private static readonly int START_HEIGHT = 200;
    
    private readonly float rDiffDown = BACKGROUND[0] - LOW_COLOUR[0];
    private readonly float gDiffDown = BACKGROUND[1] - LOW_COLOUR[1];
    private readonly float bDiffDown = BACKGROUND[2] - LOW_COLOUR[2];
    
    private readonly float rDiffUp = BACKGROUND[0] - HIGH_COLOUR[0];
    private readonly float gDiffUp = BACKGROUND[1] - HIGH_COLOUR[1];
    private readonly float bDiffUp = BACKGROUND[2] - HIGH_COLOUR[2];
    
    private float activeRotateValue = 0.0f;
    private float frameDelta = 0.0005f;
    
    private bool inTransition = false;
    private bool inVerticalTransition = false;
    private Vector3 camTransitionTarget;
    private float camDelay = 1.0f;
    private Vector3 camTransitionStart;
    private Vector3 camTransitionVector;
    private bool transitionTargetUpdated = false;
    private float targetDistance;
    private float nextTargetDistance;
    private bool searchFound = false;
    private bool viewingDir = true;
    
    private Camera cam = new Camera();
    private Vector3 camStartPosition = new Vector3(1.0f, 10.0f, -22.0f);
    private float camStartPitch = 15.0f;
    private float camStartYaw = 5.0f;
    private FileSlice sliceToFade;
    private bool fadeOut;
    private bool scaleIn = false;
    private bool doScaleIn = false;
    private FilenameCompleter completer = new FilenameCompleter();
    private EntryCompletion completion;
    private ListStore store;
    private AboutDialog about;
    private bool initted = false;
    private bool vsync = true;
    private Label label; 
    private bool textFocus;
    private float[] backgroundColour = (float[])BACKGROUND.Clone();
    private bool heightCueEnabled = true;
    private int culledThisFrame = 0;
    
       
    private LinkedList<SliceManager> sceneList = new LinkedList<SliceManager>();
    private SliceManager slices;
    private LinkedList<FileNode> selectedNodes = new LinkedList<FileNode>();
   
    
    /* Constructor */
    public MainWindow() : base(Gtk.WindowType.Toplevel) {
        Build();
        entry4.Activated += new System.EventHandler(this.OnTextEntered);
        findEntry.Activated += new System.EventHandler(this.OnSearchActivated);
        glwidget1.CanFocus = true;
        
        entry4.ModifyBase(StateType.Normal, new Gdk.Color(25, 25, 50));
        entry4.ModifyBg(StateType.Normal, new Gdk.Color(25, 25, 50));
        entry4.ModifyFg(StateType.Normal, new Gdk.Color(25, 25, 50));
        
        entry4.ModifyText(StateType.Normal, new Gdk.Color(240, 240, 240));
        entry4.ModifyCursor(new Gdk.Color(0, 240, 0), new Gdk.Color(0, 0, 255));
        
        if(GraphicsContext.ShareContexts) {
            GLWidget.GraphicsContextInitialized += new System.EventHandler(this.OnGlwidgetInit);  
            GLWidget.GraphicsContextShuttingDown += new System.EventHandler(this.OnWidgetShuttingDown);
        } else {
            glwidget1.Initialized += new System.EventHandler(this.OnGlwidgetInit);  
            glwidget1.ShuttingDown += new System.EventHandler(this.OnWidgetShuttingDown);  
        }
        
        completer.DirsOnly = true;
        completion = new EntryCompletion();
        entry4.Completion = completion;
        completion.TextColumn = 0;
        store = new ListStore(GType.String);
        completion.Model = store;
        completion.MinimumKeyLength = 1;
        glwidget1.GrabFocus();
    }
   
    /* Main Widget Init */
    protected virtual void OnGlwidgetInit(object sender, System.EventArgs e) {
        
        // open GL setup
        InitProjectionMatrix();
        
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ColorMaterial);
        GL.Enable(EnableCap.Lighting);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(CullFaceMode.Back);
        
        GL.DepthFunc(DepthFunction.Lequal);
        GL.ShadeModel(ShadingModel.Smooth);
        
        GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
        
        // set background colour
        GL.ClearColor(backgroundColour[0], backgroundColour[1], backgroundColour[2], backgroundColour[3]);
               
        GL.Fog(FogParameter.FogMode, (int)FogMode.Linear);
        GL.Fog(FogParameter.FogColor, backgroundColour);          // Set Fog Color
        GL.Fog(FogParameter.FogDensity, 0.1f);               // How Dense Will The Fog Be
        GL.Hint(HintTarget.FogHint, HintMode.Nicest);         // Fog Hint Value
        GL.Fog(FogParameter.FogStart, 130.0f);              // Fog Start Depth
        GL.Fog(FogParameter.FogEnd, 160.0f);              // Fog Start Depth               // Fog End Depth
        GL.Enable(EnableCap.Fog);
        
        // now lights
        GL.Light(LightName.Light0, LightParameter.Ambient, lightAmbient);
        GL.Light(LightName.Light0, LightParameter.Diffuse, lightDiffuse);
        GL.Light(LightName.Light0, LightParameter.Position, lightPosition);
        GL.Light(LightName.Light0, LightParameter.ConstantAttenuation, 0.8f);
        GL.Enable(EnableCap.Light0);   
        
        // setup the scene
        InitScene(); 
        GraphicsContext.CurrentContext.VSync = vsync;
        initted = true;
        Console.WriteLine(GraphicsContext.CurrentContext.GraphicsMode.ToString());
    }
    
    private void InitScene() {
        
        slices = new SliceManager(this);
        slices.Reset(START_PATH);
        
        sceneList.AddLast(slices);
        
        // Set up the camera
        cam.Put(camStartPosition, camStartPitch, camStartYaw);     
        doScaleIn = true;
        glwidget1.HasFocus = true;
        statusbar6.Push(0, " " + slices.ActiveSlice.NumFiles + " items");
        
        
        
        GLib.Idle.Add(new GLib.IdleHandler(IdleRedraw));
        //GLib.Timeout.Add (10, new GLib.TimeoutHandler (IdleRedraw));

        
    }
    
    /* Widget render callback */
    protected virtual void OnGlwidgetRenderFrame(object sender, System.EventArgs e) {
        
        if (!frameTimer.IsRunning) {
            frameTimer.Start();
        }
        
        /*if((frameTimer.ElapsedTicks - currentTicks) < 16000 ) {
            System.Threading.Thread.Sleep(16 - (int) ((frameTimer.ElapsedTicks - currentTicks)/1000.0f));  
        }*/
        culledThisFrame = 0;
        
        RenderScene();
        
        
        if (frameTimer.ElapsedMilliseconds > 1000) {
            this.Title = "GLomp " + frameCounter + "fps - " + slices.ActiveSlice.Path + " - " + culledThisFrame + " nodes culled";
            frameCounter = 0;
            currentTicks = 0;
            frameTimer.Reset();
            frameTimer.Start();
        } else {
            frameDelta = (frameTimer.ElapsedTicks - currentTicks) / 10000000.0f;
            //frameDelta = 0.01f;
            currentTicks = frameTimer.ElapsedTicks;
        }
        
        //glwidget1.QueueDraw();
        frameCounter++;   
        //GraphicsContext.CurrentContext.SwapBuffers();
        
    }

    /* My scene rendering logic */
    private void RenderScene() {
        UpdateScene();
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Matrix4 modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadMatrix(ref modelview);
        
        // apply camera transform
        cam.Transform();
   
        
        // render the nodes, just render a slice for now, it renders in order 
        foreach(SliceManager node in sceneList) {
            node.Render();
            this.culledThisFrame = node.culledTotal;
        }  
    }
    
    private void UpdateScene() {
        
        // move the camera!
        if(inTransition) {
            UpdateCamPosition();
        }
        
        // apply scale animation to fileslice
        DoScaleTransition();
        
        // rotate active box
        if(slices.ActiveSlice.NumFiles > 0) {
            slices.ActiveSlice.GetActiveNode().RotY = activeRotateValue;
        }
        
       
        activeRotateValue += 150.0f * frameDelta;

        if(activeRotateValue > 0.0f) {
            activeRotateValue -=360.0f;
        }
        
        GL.Fog(FogParameter.FogColor, backgroundColour);
        GL.ClearColor(backgroundColour[0], backgroundColour[1], backgroundColour[2], backgroundColour[3]);
        
    }
    
    public bool IdleRedraw() {
        glwidget1.QueueDraw();
        return true;
    }
    
    
    // user event handlers
    
    protected virtual void OnKeyPress (object o, Gtk.KeyPressEventArgs args) {
        
        //System.Console.WriteLine("Key Pressed - " + args.Event.KeyValue);
        if(inVerticalTransition) {
            return;
        }
        if (args.Event.Key == Gdk.Key.Tab) {
            entry4.GrabFocus();
            entry4.HasFocus = true;
            args.RetVal = true;
            return;
        }
        switch(args.Event.Key) {
        case Gdk.Key.Up: MoveForward(); args.RetVal = true; break;
        case Gdk.Key.Down: MoveBackward(); args.RetVal = true; break;
        case Gdk.Key.Left: MoveLeft(); args.RetVal = true; break;
        case Gdk.Key.Right: MoveRight(); args.RetVal = true; break;
        case Gdk.Key.Return: NodeActivated(); break;
        case Gdk.Key.BackSpace: ToParent(true); break;
        case Gdk.Key.Page_Down: ToParent(false); break;
        case Gdk.Key.Page_Up: NavUp(); break;
        case Gdk.Key.Home: slices.ActiveSlice.ReFormat(FileSlice.BY_TYPE); doScaleIn = true; break;
        case Gdk.Key.End: slices.ActiveSlice.ReFormat(FileSlice.BY_NAME); doScaleIn = true; break;
        case Gdk.Key.space: ToggleSelected(); break;
        default:
            if(args.Event.Key == Gdk.Key.f && (args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                findEntry.Visible = true;
                findEntry.HasFocus = true;
            } else if( ((IList<String>)ALPHABET).Contains(Gdk.Keyval.Name(args.Event.KeyValue).ToLower()) ) {
                slices.ActiveSlice.GoToLetter(args.Event.Key.ToString());
                ChangedActive();
                ActivateTransition();
            }
            break;
        } 
        return;
            
    }
    
    protected virtual void OnTextEntered (object o, System.EventArgs args ) {
        String path = entry4.Text;
        
        if(path.EndsWith("/") && path.Length > 1) { 
            path = path.Remove(path.Length-1); 
        }
        if (System.IO.Directory.Exists(path)) {
            NewSlice(path);
        } else {
            statusbar6.Pop(0);
            statusbar6.Push(0, " Invalid path - " + path);
        }
        
    }
    
    protected virtual void OnSearchTextChanged (object sender, System.EventArgs e)
    {
        if(slices.ActiveSlice.GoToPattern(findEntry.Text)) {
            findEntry.ModifyBase(StateType.Normal, new Gdk.Color(255, 255, 255));
            searchFound = true;
            ChangedActive();
            ActivateTransition();
        } else {
            // not found
            searchFound = false;
            findEntry.ModifyBase(StateType.Normal, new Gdk.Color(255, 30, 30));
                                
        }
    }
    
    protected virtual void OnSearchActivated (object o, System.EventArgs args ) {
        glwidget1.HasFocus = true;
        if(searchFound) {
            NodeActivated();   
        }
    }
    
    protected virtual void OnFindKeyPress (object o, Gtk.KeyPressEventArgs args)
    {
        if(args.Event.Key == Gdk.Key.Escape) {
            glwidget1.HasFocus = true;
        }
    }
   
    protected virtual void OnPathChanged (object sender, System.EventArgs e)
    {
        String[] foo = completer.GetCompletions(entry4.Text);
        if(foo.Length > 0) {
            store.Clear();
            foreach(String item in foo) {
                store.AppendValues(item);
            }
        }
    }
    
    protected virtual void OnAboutActivated (object sender, System.EventArgs e)
    {
        about = new AboutDialog();
        about.Version = "0.6";
        about.Copyright = "Copyright 2011 - Seb Clarke";
        about.License = "GPLv3 goes here!";
        about.Comments = "Special thanks to my 3d guru, Iain C, of godlike fame";
          
        about.Run();
        about.Hide();
    }
    
    protected virtual void OnNameSortActivated (object sender, System.EventArgs e)
    {
        slices.ActiveSlice.ReFormat(FileSlice.BY_NAME); 
        doScaleIn = true;
    }
    
    protected virtual void OnTypeSortActivated (object sender, System.EventArgs e)
    {
        slices.ActiveSlice.ReFormat(FileSlice.BY_TYPE); 
        doScaleIn = true;       
    }
    
    protected virtual void OnDateSortActivated (object sender, System.EventArgs e)
    {
        slices.ActiveSlice.ReFormat(FileSlice.BY_DATE);
        doScaleIn = true;
    }
    
    protected virtual void OnVsyncToggle (object sender, System.EventArgs e)
    {

        vsync = VSyncEnabledAction.Active;
        GraphicsContext.CurrentContext.VSync = vsync;
        System.Console.WriteLine("Changed vsync to " + vsync);
    }
    
    
    // system event handlers
    
    protected virtual void OnWidgetResize(object o, Gtk.SizeAllocatedArgs args) {
        if(initted) {
            ResizeProjectionMatrix(args.Allocation);
        }
    }
    
    protected void OnDeleteEvent(object sender, DeleteEventArgs a) {
        Application.Quit();
        a.RetVal = true;
    }  
    
    protected virtual void OnFindLoseFocus (object o, Gtk.FocusOutEventArgs args) {
        findEntry.Visible = false;  
        
    }
          
    
    protected virtual void OnWidgetShuttingDown (object sender, System.EventArgs e)
    {
        GL.Finish();
        GraphicsContext current = (GraphicsContext) GraphicsContext.CurrentContext;
        current.MakeCurrent(null);
        current.Dispose();    
    }
    
    
    // system convenience functions
    
    private void UpdateCamPosition() {
        // check for camera in position
        if (camDelay > 0.0f) {
            camDelay -= frameDelta;
            return;
        } else {
            camDelay = -1.0f;
        }
        
        camTransitionVector = (camTransitionTarget - camTransitionStart) * frameDelta * 4.0f;
        targetDistance = (camTransitionTarget - cam.Position).Length;
        nextTargetDistance = (camTransitionTarget - (camTransitionVector + cam.Position)).Length;
        if( ((nextTargetDistance > targetDistance) && !transitionTargetUpdated) || targetDistance < 0.3f) {
            inTransition = false;
            if (inVerticalTransition) {
                slices.ActiveSlice.ShowLabels();
            }
            if (scaleIn) {
                doScaleIn = true;
            }
            inVerticalTransition = false;
            cam.Put(camTransitionTarget);
            if(heightCueEnabled) {
                SetColourForCamHeight();
            }
            UpdateDetailsBox();
        } else{
            cam.Move((camTransitionTarget - camTransitionStart) * frameDelta * 4.0f);
            if(heightCueEnabled) {
                SetColourForCamHeight();
            }
            transitionTargetUpdated = false;
        } 
        
        if(inVerticalTransition && inTransition) {
            float alphaAdjust = frameDelta * 1.775f;
            if(fadeOut) {
                sliceToFade.Alpha -= alphaAdjust;
            } else {
                sliceToFade.Alpha += alphaAdjust;
            }
        }
        
    }
    
    private void DoScaleTransition() {
        if(doScaleIn) {
            
            if (slices.ActiveSlice.Scale > 2.0f) {
                slices.ActiveSlice.IsScaled = false;
                slices.ActiveSlice.Scale = 0.0f;
                scaleIn = false;
                doScaleIn = false;
            } else {
                slices.ActiveSlice.Scale += frameDelta * 6f;
            }
        }
        
    }
    
    private void ActivateTransition() {
        camTransitionTarget = GetCamSelectedPosition();
        camTransitionStart = cam.Position;
        if(!inTransition) {
            camDelay = 0.08f;
        }
        inTransition = true;
    }
    
    private Vector3 GetCamSelectedPosition() {        
        return slices.ActiveSlice.GetActiveNode().Position + slices.ActiveSlice.Position - CAM_OFFSET; 
    }
    
    private void ChangedActive() {
        slices.ActiveSlice.ResetVisible();
        if(slices.ActiveSlice.GetActiveNode().IsDirectory) {
            if(!viewingDir) {
                viewingDir = true;
                slices.ActiveSlice.FadeDirectories(false);
            }
            slices.ActiveSlice.ResetDirFade();
              
        } else {
            if(viewingDir) {
                viewingDir = false;
                slices.ActiveSlice.FadeDirectories(true);
                Console.WriteLine("Fading");
            }
        }
        transitionTargetUpdated = true;
        
        statusbar6.Pop(0);
        statusbar6.Push(0, " \"" + slices.ActiveSlice.GetActiveNode().FileName + "\" selected");
    }
  
    private void ResizeProjectionMatrix(Gdk.Rectangle rect) {
        Matrix4 projection;
        
        GL.Viewport(0, 0, rect.Width, rect.Height);    
        projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 8, rect.Width / (float)rect.Height, 1f, 500f);
        
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadMatrix(ref projection);
    }

    private void InitProjectionMatrix() {
        Matrix4 projection;
        if(glwidget1 == null) {
            GL.Viewport(0, 0, START_WIDTH, START_HEIGHT);
            projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 8, START_WIDTH / (float)START_HEIGHT, 1f, 500f);
        } else {
            GL.Viewport(0, 0, glwidget1.Allocation.Width, glwidget1.Allocation.Height);    
            projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 8, glwidget1.Allocation.Width / (float)glwidget1.Allocation.Height, 1f, 500f);
        }
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadMatrix(ref projection);
    }
    
    private void SetColourForCamHeight() {
        if(heightCueEnabled) {
            float camDiff = cam.Position.Y - camStartPosition.Y;
            if( camDiff > 30) {
                backgroundColour[0] = HIGH_COLOUR[0];
                backgroundColour[1] = HIGH_COLOUR[1];
                backgroundColour[2] = HIGH_COLOUR [2];
            } else if(camDiff < -30) {
                backgroundColour[0] = LOW_COLOUR[0];
                backgroundColour[1] = LOW_COLOUR[1];
                backgroundColour[2] = LOW_COLOUR[2];
            } else {
                float camCoeff = camDiff/30.0f;
                if(cam.Position.Y < camStartPosition.Y) {
                    // interpolate down
                    backgroundColour[0] = BACKGROUND[0] + (rDiffDown * camCoeff);
                    backgroundColour[1] = BACKGROUND[1] + (gDiffDown * camCoeff);
                    backgroundColour[2] = BACKGROUND[2] + (bDiffDown * camCoeff);
                        
                } else if(cam.Position.Y > camStartPosition.Y) {
                    //interpolate up
                    backgroundColour[0] = BACKGROUND[0] - (rDiffUp * camCoeff);
                    backgroundColour[1] = BACKGROUND[1] - (gDiffUp * camCoeff);
                    backgroundColour[2] = BACKGROUND[2] - (bDiffUp * camCoeff);
                    
                } else {
                    backgroundColour[0] = BACKGROUND[0];
                    backgroundColour[1] = BACKGROUND[1];
                    backgroundColour[2] = BACKGROUND[2];
                }
            }   
        } else {
            backgroundColour[0] = BACKGROUND[0];
            backgroundColour[1] = BACKGROUND[1];
            backgroundColour[2] = BACKGROUND[2];
        }
    }
 
    
    // user convenience functions 
    
    private void MoveForward() {
        if (slices.ActiveSlice.MoveCarat(0,1)) {
            ActivateTransition();
            ChangedActive();
        }
    }
    
    private void MoveBackward() {
        if (slices.ActiveSlice.MoveCarat(0,-1)) {
            ActivateTransition();
            ChangedActive();
            
        }
    }
    
    private void MoveLeft() {
        if (slices.ActiveSlice.MoveCarat(-1,0)) {
            ActivateTransition();
            ChangedActive();
        }
    }
    
    private void MoveRight() {
        if (slices.ActiveSlice.MoveCarat(1,0)) {
            ActivateTransition();
            ChangedActive();
            
        }
    }
    
    private void NavUp() {
        sliceToFade = slices.ActiveSlice;
        slices.MoveUp();
        inVerticalTransition = true;
        fadeOut = true;
        ActivateTransition();
        ChangedActive();
        statusbar6.Pop(0);
        statusbar6.Push(0, " " + slices.ActiveSlice.NumFiles + " items");
        entry4.Text = slices.ActiveSlice.Path;
    }
    
    private void NewSlice(String path) {
        // sanity checks first
        if(Directory.GetFiles(path).Length + Directory.GetDirectories(path).Length == 0) {
            glwidget1.HasFocus = true;
            statusbar6.Pop(0);
            statusbar6.Push(0, " Not Viewing Empty Folder - " + path);
            return;
        } 
        
        if(path == slices.ActivePath) {
            glwidget1.HasFocus = true;
            statusbar6.Pop(0);
            statusbar6.Push(0, " Not re-rendering - " + path);
            return;
        }
        
        // stop any active cam transitions
        inTransition = false;
        
        try {
            slices.Reset(path);
        } catch {
            slices.Reset(START_PATH);
        }
        
        cam.Put(camStartPosition);  
        SetColourForCamHeight();
        doScaleIn = true;
        glwidget1.HasFocus = true;
        statusbar6.Pop(0);
        statusbar6.Push(0, " " + slices.ActiveSlice.NumFiles + " items");
    }
        
    private void NodeActivated() {
        FileNode activeNode = slices.ActiveSlice.GetActiveNode();
        if(activeNode.IsDirectory) {
            int numFiles;
            int numFolders;
            try {
                numFiles = Directory.GetFiles(activeNode.File).Length;
                numFolders = Directory.GetDirectories(activeNode.File).Length;
            } catch(Exception e) {
                glwidget1.HasFocus = true;
                statusbar6.Pop(0);
                statusbar6.Push(0, " Error viewing folder - " + activeNode.File + " : " + e.Message);
                return;
            }
                
            if(numFiles + numFolders == 0) {
                glwidget1.HasFocus = true;
                statusbar6.Pop(0);
                statusbar6.Push(0, " Not Viewing Empty Folder - " + activeNode.File);
                return;
            }
            inVerticalTransition = true;
            fadeOut = true;
            scaleIn = true;
            sliceToFade = slices.ActiveSlice;
            slices.AddSliceAbove(activeNode.File);
            ChangedActive();
            ActivateTransition();
            statusbar6.Pop(0);
            statusbar6.Push(0, " " + slices.ActiveSlice.NumFiles + " items");
            entry4.Text = slices.ActiveSlice.Path;
        } else {
            // launch the file!            
            System.Console.WriteLine("Launching " + activeNode.File);
            // TODO: Launch using GIO
            System.Diagnostics.Process.Start(activeNode.File);
            statusbar6.Pop(0);
            statusbar6.Push(0, " Launched " + activeNode.File);
            
        }
    }
    
    private void ToParent(bool clearAbove) {
        inTransition = false;
       
        if(clearAbove) {
            slices.MoveDownClear();
        } else {
            slices.MoveDown();   
        }
        sliceToFade = slices.ActiveSlice;
  
        glwidget1.HasFocus = true;
        fadeOut = false;
        inVerticalTransition = true;
        ChangedActive();
        ActivateTransition();
        statusbar6.Pop(0);
        statusbar6.Push(0, " " + slices.ActiveSlice.NumFiles + " items");
        entry4.Text = slices.ActiveSlice.Path;
    }
        
    protected virtual void OnHeightColourToggle (object sender, System.EventArgs e)
    {
        Console.WriteLine("Height Colour Toggled");
        heightCueEnabled = !heightCueEnabled;
        SetColourForCamHeight();
    }
    
    
    // external convenience functions
    public Camera GetCamera() {
        return cam;
    }
    
    protected virtual void OnWidgetClick (object o, Gtk.ButtonReleaseEventArgs args)
    {  
        Console.WriteLine("Widget clicked");
        glwidget1.GrabFocus();
        glwidget1.HasFocus = true;
    }
    
    private void UpdateDetailsBox() {
        if(detailsBox.Visible) {
            FileNode active = slices.ActiveSlice.GetActiveNode();
            GLib.File activeFile = GLib.FileFactory.NewForPath(active.File);
            if(!active.IsDirectory) {
                detailLabelContents.Visible = false;
                detailValueContents.Visible = false;
                
                
                detailEntryName.Text = active.FileName;
                
                
                String infoString = "standard::size,standard::content-type,filesystem:free,time::modified,time::access,owner::user,owner::group";
                GLib.FileInfo info = activeFile.QueryInfo(infoString, FileQueryInfoFlags.None, null);
                
                String sizeString = info.GetAttributeAsString("standard::size");
                detailValueSize.Text = String.Format(new FileSizeFormatProvider(), "{0:fs}", Convert.ToUInt64(sizeString));
                
                detailValueType.Text = Gnome.Vfs.Mime.GetDescription(info.ContentType) + " (" + info.ContentType + ")";
                
                DateTime tempTime = NodeManager.ConvertFromUnixTimestamp(Convert.ToUInt64(info.GetAttributeAsString("time::access")));
                detailValueAccessed.Text = tempTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss");
                
                tempTime = NodeManager.ConvertFromUnixTimestamp(Convert.ToUInt64(info.GetAttributeAsString("time::modified")));
                detailValueModified.Text = tempTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss");
                
                detailValueSpace.Text = String.Format(new FileSizeFormatProvider(), "{0:fs}",
                                                      Convert.ToUInt64(info.GetAttributeString("filesystem:free")));
                
                detailValueOwner.Text = info.GetAttributeString("owner::user");
                detailValueGroup.Text = info.GetAttributeString("owner::group");
                
                
            } else {
                
                detailLabelContents.Visible = true;
                detailValueContents.Visible = true;
                detailValueSize.Text = "[unsupported]";
                
                detailEntryName.Text = active.FileName;
                detailValueType.Text = "Directory";
                
                String infoString = "time::modified,time::access,owner::user,owner::group";
                GLib.FileInfo info = activeFile.QueryInfo(infoString, FileQueryInfoFlags.None, null);
                
                DateTime tempTime = NodeManager.ConvertFromUnixTimestamp(Convert.ToUInt64(info.GetAttributeAsString("time::access")));
                detailValueAccessed.Text = tempTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss");
                
                tempTime = NodeManager.ConvertFromUnixTimestamp(Convert.ToUInt64(info.GetAttributeAsString("time::modified")));
                detailValueModified.Text = tempTime.ToString("ddd, dd MMM yyyy HH':'mm':'ss");
                
                
                detailValueContents.Text = active.NumChildren + " items (" + active.NumDirs + " folders)";
                
                detailValueOwner.Text = info.GetAttributeString("owner::user");
                detailValueGroup.Text = info.GetAttributeString("owner::group");
                
            }
            Mono.Unix.Native.Statvfs fsbuf = new Mono.Unix.Native.Statvfs();
            Mono.Unix.Native.Syscall.statvfs(slices.ActiveSlice.GetActiveNode().File, out fsbuf);

            
            detailValueSpace.Text = String.Format(new FileSizeFormatProvider(), "{0:fs}", fsbuf.f_bavail * fsbuf.f_bsize);
            UpdateDetailPermissions();
            if(detailValueOwner.Text == Mono.Unix.UnixUserInfo.GetRealUser().UserName) {
                EnableDetailPermissions(true);
            } else {
                EnableDetailPermissions(false);
            }
        }
        
    }
    
    private void UpdateDetailPermissions() {

        Stat buf = new Mono.Unix.Native.Stat() ;
        
        Syscall.stat(slices.ActiveSlice.GetActiveNode().File, out buf);
        
        permissionCheckOR.Active = false;
        permissionCheckGR.Active = false;
        permissionCheckUR.Active = false;
        permissionCheckOW.Active = false;
        permissionCheckGW.Active = false;
        permissionCheckUW.Active = false;
        permissionCheckOX.Active = false;
        permissionCheckGX.Active = false;
        permissionCheckUX.Active = false;
        
        if((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IRUSR) != 0) {
            permissionCheckOR.Active = true;
        }
        if((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IRGRP) != 0) {
            permissionCheckGR.Active = true;
        }
        if((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IROTH) != 0) {
            permissionCheckUR.Active = true;
        }
        if((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IWUSR) != 0) {
            permissionCheckOW.Active = true;
        }
        if((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IWGRP) != 0) {
            permissionCheckGW.Active = true;
        }
        if((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IWOTH) != 0) {
            permissionCheckUW.Active = true;
        }
        if((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IXUSR) != 0) {
            permissionCheckOX.Active = true;
        }
        if((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IXGRP) != 0) {
            permissionCheckGX.Active = true;
        }
        if((buf.st_mode & Mono.Unix.Native.FilePermissions.S_IXOTH) != 0) {
            permissionCheckUX.Active = true;
        }
        
    }
    
    private void EnableDetailPermissions(bool enable) {

        permissionCheckOR.Sensitive = enable;
        permissionCheckGR.Sensitive = enable;
        permissionCheckUR.Sensitive = enable;
        permissionCheckOW.Sensitive = enable;
        permissionCheckGW.Sensitive = enable;
        permissionCheckUW.Sensitive = enable;
        permissionCheckOX.Sensitive = enable;
        permissionCheckGX.Sensitive = enable;
        permissionCheckUX.Sensitive = enable;

    }
    
    private void ApplyNewPermissions() {
        FilePermissions toSet = 0;
        
        if(permissionCheckOR.Active) {
            toSet = toSet | FilePermissions.S_IRUSR;    
        }
        if(permissionCheckGR.Active) {
            toSet = toSet | FilePermissions.S_IRGRP;
        }
        if(permissionCheckUR.Active) {
            toSet = toSet | FilePermissions.S_IROTH;
        }
        if(permissionCheckOW.Active) {
            toSet = toSet | FilePermissions.S_IWUSR;
        }
        if(permissionCheckGW.Active) {
            toSet = toSet | FilePermissions.S_IWGRP;
        }
        if(permissionCheckUW.Active) {
            toSet = toSet | FilePermissions.S_IWOTH;
        }
        if(permissionCheckOX.Active) {
            toSet = toSet | FilePermissions.S_IXUSR;
        }
        if(permissionCheckGX.Active) {
            toSet = toSet | FilePermissions.S_IXGRP;
        }
        if(permissionCheckUX.Active) {
            toSet = toSet | FilePermissions.S_IXOTH;
        }
        
        Syscall.chmod(slices.ActiveSlice.GetActiveNode().File, toSet);
    }
    
    protected virtual void OnDetailsVisibleToggled (object sender, System.EventArgs e)
    {
        detailsBox.Visible = !detailsBox.Visible;
        UpdateDetailsBox();
    }
    
    protected virtual void OnPermissionsToggle (object sender, System.EventArgs e)
    {
        ApplyNewPermissions();
    }
    
    private void RenameFile(String newFileName) {
        // rename the file
        FileNode active = slices.ActiveSlice.GetActiveNode();
        
        try {
            System.IO.File.Move(active.File, active.File.Replace(active.FileName, newFileName));
        } catch {
            statusbar6.Pop(0);
            statusbar6.Push(0, " Failed to rename file");
            return;
        }
        slices.ActiveSlice.RenameActiveNode(newFileName);
        
    }
    
    private void ToggleSelected() {
        if(slices.ActiveSlice.ToggleSelected()) {
            selectedNodes.AddLast(slices.ActiveSlice.GetActiveNode());
        } else {
            selectedNodes.Remove(slices.ActiveSlice.GetActiveNode());
        }

    }
    
    protected virtual void OnNewNameEntered (object sender, System.EventArgs e)
    {
        RenameFile(detailEntryName.Text);
    }
    
    
    
    
    
    
    
}
