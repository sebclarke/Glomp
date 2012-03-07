
using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace glomp {


    public class SliceManager : SceneNode {
        
        private FileSlice activeSlice;
        private int activeSliceHeight = 0;
        public static readonly float SLICE_SPACING = 10.0f;
        private MainWindow parentWindow;
        private FrustumCuller culler;
        public int culledTotal = 0;
     
        private LinkedList<FileSlice> slices = new LinkedList<FileSlice>();
        private LinkedListNode<FileSlice> activeSliceNode;
        
        public String ActivePath {
            get { return activeSlice.Path; }
        }
        
        
        public FileSlice ActiveSlice {
            get { return activeSlice; }
        }
            
        
        public int ActiveHeight {
            get { return activeSliceHeight; }
        }

        public SliceManager(MainWindow parent) {
            parentWindow = parent;
            culler = new FrustumCuller();
        }
        
        public int Reset(String path) {
            // destroy gl textures
            foreach(var slice in slices) {
                slice.Destroy();
            }
            // destroy slices
            slices.Clear();
            
            // reset height
            activeSliceHeight = 0;
            
            // make new fileSlice, set to active
            activeSlice = new FileSlice(path, activeSliceHeight, parentWindow);
            activeSlice.Scale = 0f;
            activeSlice.IsScaled = true;
            
            // add to slice list
            activeSliceNode = slices.AddLast(activeSlice);
            
            return activeSliceHeight;
        }
        
        public void AddSliceAbove(String path) {
            // destroy all existing slices above us
            while(activeSliceNode != slices.Last) {
                slices.Last.Value.Destroy();
                slices.RemoveLast();
            }
            
            // make new slice
            FileSlice upperSlice = new FileSlice(path, activeSliceHeight +1, parentWindow);
            
            // set its position
            Vector3 newSlicePosition = activeSlice.Position + (Vector3.UnitY * SLICE_SPACING);
            
            // it is now directly above us, move it to line up with parent
            newSlicePosition.Z += activeSlice.ActiveBox[1] * 6.0f;
            newSlicePosition.X -= activeSlice.ActiveBox[0] * 6.0f;
            
            upperSlice.Position = newSlicePosition;
            upperSlice.ResetVisible();
            
            activeSlice.HideLabels();
            
            activeSliceNode = slices.AddLast(upperSlice);
            activeSlice = upperSlice;
            activeSlice.Scale = 0f;
            activeSlice.IsScaled = true;
            
            activeSliceHeight++;
        }
        
        public int AddSliceBelow() {
            String parentPath;
            
            // check if we are at the root
            try {
                parentPath = Directory.GetParent(activeSlice.Path).FullName;
            } catch {
                return activeSliceHeight;
            }
            
            // make new slice
            FileSlice lowerSlice = new FileSlice(parentPath, activeSliceHeight -1, parentWindow);
            
            // set its position
            Vector3 newSlicePosition = activeSlice.Position - (Vector3.UnitY * SLICE_SPACING);
            
            // it is now directly below us, move it to line up with parent
            lowerSlice.ActivateNode(activeSlice.Path);
            newSlicePosition.Z -= lowerSlice.ActiveBox[1] * 6.0f;
            newSlicePosition.X += lowerSlice.ActiveBox[0] * 6.0f;
            
            lowerSlice.Position = newSlicePosition;
            lowerSlice.ResetVisible();
            
            activeSliceNode = slices.AddFirst(lowerSlice);
            
            activeSlice = lowerSlice;
            
            activeSliceHeight--;
            
            foreach(var slice in slices) {
                if(slice.SliceHeight - activeSliceHeight >= 2) {
                    slice.Visible = false;
                }
            }
            
            return activeSliceHeight;
        }
        
        public void MoveUp() {
            // Check there is a slice above us to move up to
            if(activeSliceNode == slices.Last) {
                System.Console.WriteLine("Denying move up, no upper slice!");
                return;
            }
            //before doing anything set the slice 2 above to visible
            if(activeSliceNode.Next.Next != null) {
                activeSliceNode.Next.Next.Value.Visible = true;
            }
            
            activeSlice.HideLabels();
            
            // set slice above us to be active
            activeSlice = activeSliceNode.Next.Value;
            activeSliceNode = activeSliceNode.Next;
            activeSlice.Activate();
            
            
            activeSliceHeight++;
            
            // TODO: Destroy textures of slices more than 1 below
            
            
            
        }
        
        public void MoveDown() {
            // check if there is slice below, if not create it!
            if(activeSliceNode == slices.First) {
                System.Console.WriteLine("Creating new slice below");
                AddSliceBelow();
            } else {
                // just move down
                activeSlice.DeActivate();
                activeSliceNode.Previous.Value.ActivateNode(activeSlice.Path);
                activeSlice = activeSliceNode.Previous.Value;
                activeSliceNode = activeSliceNode.Previous;
                //activeSlice.ShowLabels();
                activeSliceHeight--;
                
                // TODO: Destroy textures of slice more than 1 above
                // for now just dont render

                foreach(var slice in slices) {
                    if(slice.SliceHeight - activeSliceHeight >= 2) {
                        slice.Visible = false;
                    }
                }
            }
            
        }
        
        public void MoveDownClear() {
            MoveDown();
            while(activeSliceNode != slices.Last) {
                slices.Last.Value.Destroy();
                slices.RemoveLast();
            }
            
        }
       
        
        public override void Render() {
            float[] model = new float[16];
            float[] proj = new float[16];
            culledTotal = 0;
            GL.GetFloat(GetPName.ProjectionMatrix, proj);
            GL.GetFloat(GetPName.ModelviewMatrix, model);
            culler.CalculateFrustum(model, proj);
            
            foreach(FileSlice slice in slices) {
                slice.Render(culler);
                culledTotal += slice.cullCount;
            }
        }
    }
}
