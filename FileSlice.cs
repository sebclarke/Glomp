
using System;
using System.IO;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace glomp {


    public class FileSlice : SceneNode {
        
        private static readonly int X = 0;
        private static readonly int Y = 1;  
        private static readonly int leftVisible = -3;
        private static readonly int rightVisible = 3;
        private static readonly int forwardVisible = 8;
        private static readonly int backVisible = -2;
        
        private static readonly float STARTX = 6.0f;
        private static readonly float STARTY = -2.0f;
        private static readonly float STARTZ = 10.0f;
        private static readonly float ASPECT_COEFF = 1.6f;     
        private static readonly int SHOW_ALL_LIMIT = 400;
        
        public static readonly float BOX_SPACING = 6.0f;  
        public static readonly Vector3 NO_VECTOR = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        
        public static readonly int BY_TYPE = 0;
        public static readonly int BY_NAME = 1;
        public static readonly int BY_DATE = 2;
        
        public Vector3 camOffset = new Vector3(5.0f, -12.0f, 32.0f);
        
        
        private int gridWidth;
        private int gridHeight;
        private int[] activeBox = {0,0};
        private bool showAllText = false;      
        private FileNode toNode;
        private int numFiles;
        private String path;
        private int sliceHeight;
        private float alpha = 0.5f;
        private bool isDimmed = false;
        private int lastLetterPosition = -1;
        private String lastLetter = "";
        private float scale = 0.0f;
        private bool isScaled = false;
        private int currentSortType = BY_NAME;
        private bool visible = true;
        private MainWindow parentWindow;
        public int cullCount = 0;
        
        
        public FileNode[] fileNodes;
        public bool showHidden = false;
        
        public float Alpha {
            get { return alpha; }
            set {
                if(value > 1.0f) {
                    alpha = 1.0f;
                } else if(value < 0.0f) {
                    alpha = 0.0f;
                } else {
                    alpha = value;
                }
            }
        }
        
       
        
        public bool Visible {
            get { return visible; }
            set { visible = value; }
        }
        
        public int CurrentSortType {
            get { return currentSortType;}
        }
        
        public float Scale {
            get { return scale;}
            set { scale = value;}
        }
        
        public bool IsScaled {
            get { return isScaled;}
            set { isScaled = value;}
        }
  
        public String Path {
            get { return path; }
        }
        
        public int SliceHeight {
            get { return sliceHeight; }
        }
        
        public int[] ActiveBox {
            get { return activeBox; }
        }
        
        public int NumFiles {
            get { return numFiles; }
        }
        
        public FileSlice(String _path, int _sliceHeight, MainWindow parent) 
            : base () {
            path = _path;
            sliceHeight = _sliceHeight;
            parentWindow = parent;
            
            LinkedList<String> files = new LinkedList<String>();
            LinkedList<String> folders = new LinkedList<String>();
            
            // prune hidden files
            if(!showHidden) {
                String[] rawFiles = Directory.GetFiles(path);
                String[] rawFolders = Directory.GetDirectories(path);
                Array.Sort(rawFiles);
                Array.Sort(rawFolders);

                foreach(var file in rawFiles) {
                    if(! file.Replace(path, "").StartsWith("/.")) {
                        files.AddLast(file);
                    }
                }
                foreach(var file in rawFolders) {
                    if(! file.Replace(path, "").StartsWith("/.")) {
                        folders.AddLast(file);
                    }         
                }
                
            } else {
                files = new LinkedList<String>(Directory.GetFiles(path));
                folders = new LinkedList<String>(Directory.GetDirectories(path));
            }
            
            // set up storage
            fileNodes = new FileNode[files.Count + folders.Count];
            numFiles = fileNodes.Length;

            // decide if we will show all the labels, or just the ones near us
            if(fileNodes.Length < SHOW_ALL_LIMIT) {
                showAllText = true;
            }
         
            int nodeCount = 0;
            if(numFiles > 0) {
                // set up width and height in boxes
                gridWidth = (int)Math.Round(Math.Sqrt(fileNodes.Length)/ASPECT_COEFF, 0);
                gridHeight = fileNodes.Length / gridWidth;
                if(fileNodes.Length % gridHeight > 0)
                    gridHeight += 1;
                
                // generate the nodes
                foreach(var folder in folders) {
                    FileNode node = NodeManager.GetFileNode(NodeManager.DIR_NODE, folder, this);
                    float xPosition = STARTX - ((nodeCount % gridWidth) * BOX_SPACING);
                    float zPosition = STARTZ + ((nodeCount / gridWidth) * BOX_SPACING);
                    node.Position = new Vector3(xPosition, STARTY, zPosition);
                    if(nodeCount == 0) {
                        node.Active = true;
                    }
                    fileNodes[nodeCount] = node;
                    nodeCount++;
                }
                
                foreach(var file in files) {
                    FileNode node = NodeManager.GetFileNode(NodeManager.FILE_NODE, file, this);
                    float xPosition = STARTX - ((nodeCount % gridWidth) * BOX_SPACING);
                    float zPosition = STARTZ + ((nodeCount / gridWidth) * BOX_SPACING);
    
                    node.Position = new Vector3(xPosition, STARTY, zPosition);
                    if(nodeCount == 0) {
                        node.Active = true;
                    }
                    fileNodes[nodeCount] = node;
                    nodeCount++;
                }
            }            
            if(showAllText) {
                GenerateAllTextures();
            } else {
                ResetVisible();
            }
               
        }
        
        
        public void ReFormat(int sortBy) {
            currentSortType = sortBy;
            GetActiveNode().Active = false;
            if(sortBy == BY_TYPE) {
                // re sort filenode array by descritpion
                SortType();
            } else if(sortBy == BY_NAME) {
                SortName();                
            } else if(sortBy == BY_DATE) {
                SortDate();
            }
            
            // set positions based on new order
            for(int i = 0; i < fileNodes.Length; i++) {
                float xPosition = STARTX - ((i % gridWidth) * BOX_SPACING);
                float zPosition = STARTZ + ((i / gridWidth) * BOX_SPACING);
                fileNodes[i].Position = new Vector3(xPosition, STARTY, zPosition);
                        
            }
            fileNodes[(activeBox[1] * gridWidth) + activeBox[0]].Active = true;
            scale = 0f;
            isScaled = true;
        }

        
        public void ResetVisible() {
            // caclulate x,ys for visible nodes
            if(showAllText) {
                return;
            }
            int[] minBox = { activeBox[X] + leftVisible, activeBox[Y] + backVisible };
            int[] maxBox = { activeBox[X] + rightVisible, activeBox[Y] + forwardVisible };
            
            int genCounter = 0;
            int destCounter = 0;
            FileNode myNode = null;
            
            // for all visible nodes
            for(int y = minBox[Y]; y < maxBox[Y]; y++) {
                // sanity check
                if((y < 0 || y > gridHeight-1))
                    continue;
                        
                for(int x = minBox[X]; x < maxBox[X]; x++) {
                    // sanity check
                    if((x < 0 || x > gridWidth-1))
                        continue;
                    
                    // try to get a fileNode for these coords
                    try {
                        myNode = fileNodes[(y * gridWidth) + x];
                    } catch {
                        continue;
                    }
                   
                    if(!myNode.Visible) {
                        myNode.Visible = true;
                        myNode.GenTexture(false);
                        genCounter++;
                    }
                }       
            }
                     
            // for all other positions
            for(int i = 0; i < fileNodes.Length; i++) {
                int x = i % gridWidth;
                int y = i / gridWidth;
                
                // dont include visible ones!
                if( (y >= minBox[Y] && y <= maxBox[Y]) && (x >= minBox[X] && x <= maxBox[X]) ) {
                        continue;
                }
                
                myNode = fileNodes[i];
                
                if(myNode.Visible) {
                        myNode.Visible = false;
                        myNode.DestroyTexture();
                        destCounter++;
                 }
            }
            
            System.Console.WriteLine("Generated " + genCounter + " textures.");
            System.Console.WriteLine("Destroyed " + destCounter + " textures.");
        }
        
     
        public void Render(FrustumCuller culler) {
            if(visible) {
                // render the array in reverse order, first boxes then labels
                float sliceX, sliceY, sliceZ;
                sliceX = this.position.X;
                sliceY = this.position.Y;
                sliceZ = this.position.Z;
                cullCount = 0;
                
                GL.PushMatrix();
                MoveIntoPosition(false);
                FileNode.SetBoxState(isDimmed);
                for(int i = fileNodes.Length-1; i >= 0; i--) {
                    float nodeZ = sliceZ + fileNodes[i].Position.Z;
                    float nodeX = sliceX + fileNodes[i].Position.X - 2.1f;
                    float nodeY = sliceY + fileNodes[i].Position.Y;
                    if( (nodeZ > parentWindow.GetCamera().Position.Z) && 
                       (culler.isBoxVisible(nodeX, nodeY, nodeZ, 3f, 8f)) ) {
                        
                        
                        fileNodes[i].culled = false;
                        fileNodes[i].DrawBox(i);
                    } else {
                        fileNodes[i].culled = true;
                        cullCount++;       
                    } 
                }
                FileNode.UnsetBoxState(isDimmed);
                
                FileNode.SetTextState(isDimmed);
                for(int i = fileNodes.Length-1; i >= 0; i--) {
                    if(!fileNodes[i].culled) {
                        fileNodes[i].DrawLabel();
                    }
                }
                FileNode.UnsetTextState(isDimmed);
                GL.PopMatrix();
            }
        }
        
        public override void Render() {
            // render without culler
        }
        
        
        public FileNode GetActiveNode() {
            if(fileNodes.Length > 0)
                return fileNodes[(activeBox[Y] * gridWidth) + activeBox[X]];
            else 
                return new FileNode(this.Position);
        }
        
        
        public bool MoveCarat(int xMove, int yMove) {
            // check that we stay in our grid
            int targetX = activeBox[X] + xMove;
            int targetY = activeBox[Y] + yMove;
            if( (targetX < 0 || targetX > gridWidth-1) || (targetY < 0 || targetY > gridHeight-1) ) {
                return false;
            }
            
            // check that we have a file under us
            try {
                toNode = fileNodes[(targetY * gridWidth) + targetX];
            } catch {
                return false;
            }
            
            
            fileNodes[(activeBox[Y] * gridWidth) + activeBox[X]].Active = false;
            activeBox[X] += xMove;
            activeBox[Y] += yMove;
            toNode.Active = true;
            return true;
        }
        
        
        private void GenerateAllTextures() {
            foreach(var node in fileNodes) {
                node.Visible = true;
                node.GenTexture(false);
            }
        }
        
        
        public void Destroy() {
            foreach(var node in fileNodes) {
                node.DestroyTexture();
            }
        }
        
        
        public Vector3 FindNodePosition(String file) {
            for( int i = 0; i < fileNodes.Length; i++) {
                if(fileNodes[i].File == file) {
                    System.Console.WriteLine("Found " + file);
                    return fileNodes[i].Position;
                }
            }
            return NO_VECTOR;
        }
        
        
        public Vector3 ActivateNode(String file) {
           for( int i = 0; i < fileNodes.Length; i++) {
                if(fileNodes[i].File == file) {
                    System.Console.WriteLine("Found " + file);
                    GoToNode(i);
                    return fileNodes[i].Position;
                }
            }

            return NO_VECTOR;
        }
        
        
        public void DeActivate() {
            GetActiveNode().Active = false;
        }
        
        
        public void Activate() {
            GetActiveNode().Active = true;
        }
        
        
        public void GoToLetter(String letter) {
            letter = letter.ToLower();
            if(letter == lastLetter) {
                for(int i = lastLetterPosition + 1; i < fileNodes.Length; i++) {
                   if( fileNodes[i].FileName.StartsWith(letter,true, null) ) {
                        lastLetter = letter;
                        lastLetterPosition = i;
                        GoToNode(i);
                        return;
                    }    
                }
            }            
            for( int i = 0; i < fileNodes.Length; i++ ) {
                if( fileNodes[i].FileName.StartsWith(letter, true, null) ) {
                    lastLetter = letter;
                    lastLetterPosition = i;
                    GoToNode(i);
                    return;
                }
            }
        }
        
        
        public bool GoToPattern(String pattern) {
            pattern = pattern.ToLower();
            for(int i = 0; i < fileNodes.Length; i++ ) {
                if(fileNodes[i].FileName.StartsWith(pattern, true, null)) {
                   GoToNode(i);
                    return true;
                }
            }
            for(int i = 0; i < fileNodes.Length; i++ ) {
                if(fileNodes[i].FileName.ToLower().Contains(pattern)) {
                   GoToNode(i);
                    return true;
                }
            }
            return false;    
        }
        
        
        public void GoToNode(int position) {
            GetActiveNode().Active = false;
            activeBox[0] = position % gridWidth;
            activeBox[1] = position / gridWidth;
            fileNodes[position].Active = true;
            ResetVisible();
        }
        
        
        public void HideLabels() {
            foreach(var fileNode in fileNodes) {
                fileNode.Dimmed = true;
            }
            isDimmed = true;
        }
        
        
        public void ShowLabels() {
            alpha = 0.5f;
            foreach(var fileNode in fileNodes) {
                fileNode.Dimmed = false;
            }
            isDimmed = false;
        }
        
        
        private void SortName() {
            Array.Sort(fileNodes, delegate(FileNode node1, FileNode node2) {
                if(node1.IsDirectory == node2.IsDirectory) {
                    return node1.FileName.CompareTo(node2.FileName);
                } else {
                    if(node1.IsDirectory) {
                        return -1;
                    } else {
                        return 1;
                    }
                }
            });          
        }
        
        
        private void SortType() {
            Array.Sort(fileNodes, delegate(FileNode node1, FileNode node2) {
                int compareVal = node1.Description.CompareTo(node2.Description);
                if(compareVal == 0) {
                    String ext1, ext2;
                    try { 
                        String[] split1 = node1.FileName.Split('.');
                        String[] split2 = node2.FileName.Split('.');
                        ext1 = split1[split1.Length-1];
                        ext2 = split2[split2.Length-1];
                    } catch {
                        return node1.FileName.CompareTo(node2.FileName);
                    }
                    compareVal = ext1.CompareTo(ext2);
                    if(compareVal == 0) {
                        return node1.FileName.CompareTo(node2.FileName);
                    } else {
                        return compareVal;
                    }
                } else {
                    return compareVal;
                }
            });   
        }
        
        private void SortDate() {
            Array.Sort(fileNodes, delegate(FileNode node1, FileNode node2) {
                if(node1.IsDirectory == node2.IsDirectory) {
                    return node1.ModifyTime.CompareTo(node2.ModifyTime);
                } else {
                    if(node1.IsDirectory) {
                        return -1;
                    } else {
                        return 1;
                    }
                }
            }); 
        }
        
        public void FadeDirectories(bool fadeOut) {
            foreach(FileNode node in fileNodes) {
                if(node.IsDirectory) {
                    node.DirFaded = fadeOut;
                    if(fadeOut) {
                        node.FadeAmount = 0.1f;
                    }
                }
            }
        }
        
        public void ResetDirFade() {
            for(int i = 0; i < fileNodes.Length; i++) {
                if(fileNodes[i].IsDirectory) {
                    if(i / gridWidth < activeBox[1])  { // if we are behind current active
                        fileNodes[i].DirFaded = true;
                        fileNodes[i].FadeAmount = 0.3f;
                        
                    } else {
                        fileNodes[i].DirFaded = false;
                    }
                }
            }
        }
        
        public void RenameActiveNode(String newFileName) {
            String oldFileName = fileNodes[(activeBox[Y] * gridWidth) + activeBox[X]].FileName;
            fileNodes[(activeBox[Y] * gridWidth) + activeBox[X]].FileName = newFileName;
            fileNodes[(activeBox[Y] * gridWidth) + activeBox[X]].File = fileNodes[(activeBox[Y] * gridWidth) + activeBox[X]].File.Replace(oldFileName, newFileName);
            fileNodes[(activeBox[Y] * gridWidth) + activeBox[X]].GenTexture(true);
            
        }
        
        public bool ToggleSelected() {
            fileNodes[(activeBox[Y] * gridWidth) + activeBox[X]].Selected = !fileNodes[(activeBox[Y] * gridWidth) + activeBox[X]].Selected;
            return fileNodes[(activeBox[Y] * gridWidth) + activeBox[X]].Selected;
            
        }
        
        
        
    }
}
