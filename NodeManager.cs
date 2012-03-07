
using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.IO;
using System.Runtime.InteropServices;

namespace glomp {


    public class NodeManager {
        
        private static int[] displayLists = new int[2];
        private static bool[] listGenerated = {false, false};
        
        public static readonly int FILE_NODE = 0;
        public static readonly int DIR_NODE = 1;
        private static readonly float BOX_SCALE = 0.8f;
        private static readonly String COLOUR_SALT = "";
        

        public NodeManager() {
        }
        
        public static FileNode GetFileNode(int nodeType, String fileName, FileSlice owner) {
            int fileDisplayList;
            
            if(listGenerated[nodeType]) {
                fileDisplayList = displayLists[nodeType];
            } else {
                fileDisplayList = displayLists[nodeType] = GenerateDisplayList(nodeType);
            }
            
           
            GLib.File fi = GLib.FileFactory.NewForPath(fileName);
            
            FileNode node = new FileNode(fi.Basename);
            node.File = fileName;
            //node.NumChildren = fi.
            GLib.FileInfo info = fi.QueryInfo("access::can-execute,thumbnail::path,filesystem::readonly,time::modified", GLib.FileQueryInfoFlags.None, null);
            node.SetDisplayList(fileDisplayList);
            
            node.SetParent(owner);
            
            if(nodeType == DIR_NODE) {
                node.IsDirectory = true;
                try {
                    node.NumDirs = Directory.GetDirectories(fileName).Length;
                    node.NumFiles = Directory.GetFiles(fileName).Length;
                    node.NumChildren = node.NumDirs + node.NumFiles;
                    node.DirHeight = GetHeightForFolder(node.NumChildren);
                } catch {
                    node.NumChildren = 0;
                    node.DirHeight = 1f;
                }
            } else { 
                String description = Gnome.Vfs.Mime.GetDescription(fi.QueryInfo("standard::content-type", GLib.FileQueryInfoFlags.None, null).ContentType);
                if(description == null) {
                    // use the extension
                    String[] split = node.FileName.Split('.');
                    if(split.Length > 1) {
                        description = split[split.Length-1] + " file";
                    } else {
                        // no extension either
                        description = "unknown";
                    }
                } 
                node.Description = description;

                if(info.GetAttributeBoolean("filesystem::readonly")) {
                    node.IsReadOnly = true;   
                } else if(info.GetAttributeBoolean("access::can-execute")) {
                    node.IsExecutable = true;
                } else {
                    node.TypeColour = NodeManager.GetColourForNode(node);   
                }
            }
           
            
            node.ModifyTime = ConvertFromUnixTimestamp(Convert.ToUInt64(info.GetAttributeAsString("time::modified")));
            //Console.WriteLine(node.File + " : " + node.ModifyTime.ToString("MMMM dd, yyyy"));

            string previewPath = info.GetAttributeByteString("thumbnail::path");
            if(previewPath != null) {
                node.ThumbFileName = previewPath;
            }
            
            return node;
        }
        
        
        private static int GenerateDisplayList(int nodeType) {
            System.Console.WriteLine("Genning new list");
            int displayList = GL.GenLists(1);
            if(nodeType == FILE_NODE) {
                
                GL.NewList(displayList, ListMode.Compile); // start compiling display list
                //GL.Color3(boxColour[0], boxColour[1], boxColour[2]);
                //GL.Scale(BOX_SCALE, BOX_SCALE, BOX_SCALE);
                GL.Begin(BeginMode.Quads);          // start drawing quads
                
                // Front Face
                GL.Normal3( 0.0f, 0.0f, 1.0f);      // Normal Facing Forward
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(-0.8f, -0.8f,  0.8f);  // Bottom Left Of The Texture and Quad
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3( 0.8f, -0.8f,  0.8f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3( 0.8f,  0.8f,  0.8f);  // Top Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(-0.8f,  0.8f,  0.8f);  // Top Left Of The Texture and Quad
                // Back Face
                GL.Normal3( 0.0f, 0.0f,-1.0f);      // Normal Facing Away
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(-0.8f, -0.8f, -0.8f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(-0.8f,  0.8f, -0.8f);  // Top Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3( 0.8f,  0.8f, -0.8f);  // Top Left Of The Texture and Quad
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3( 0.8f, -0.8f, -0.8f);  // Bottom Left Of The Texture and Quad
                // Top Face
                GL.Normal3( 0.0f, 1.0f, 0.0f);      // Normal Facing Up
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(-0.8f,  0.8f, -0.8f);  // Top Left Of The Texture and Quad
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(-0.8f,  0.8f,  0.8f);  // Bottom Left Of The Texture and Quad
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3( 0.8f,  0.8f,  0.8f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3( 0.8f,  0.8f, -0.8f);  // Top Right Of The Texture and Quad
                // Bottom Face
                GL.Normal3( 0.0f,-1.0f, 0.0f);      // Normal Facing Down
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(-0.8f, -0.8f, -0.8f);  // Top Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3( 0.8f, -0.8f, -0.8f);  // Top Left Of The Texture and Quad
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3( 0.8f, -0.8f,  0.8f);  // Bottom Left Of The Texture and Quad
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(-0.8f, -0.8f,  0.8f);  // Bottom Right Of The Texture and Quad
                // Right face
                GL.Normal3( 1.0f, 0.0f, 0.0f);      // Normal Facing Right
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3( 0.8f, -0.8f, -0.8f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3( 0.8f,  0.8f, -0.8f);  // Top Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3( 0.8f,  0.8f,  0.8f);  // Top Left Of The Texture and Quad
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3( 0.8f, -0.8f,  0.8f);  // Bottom Left Of The Texture and Quad
                // Left Face
                GL.Normal3(-1.0f, 0.0f, 0.0f);      // Normal Facing Left
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(-0.8f, -0.8f, -0.8f);  // Bottom Left Of The Texture and Quad
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(-0.8f, -0.8f,  0.8f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(-0.8f,  0.8f,  0.8f);  // Top Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(-0.8f,  0.8f, -0.8f);  // Top Left Of The Texture and Quad
                
                GL.End();                    // Done Drawing Quads
                
                GL.EndList();                // Finish display list
                listGenerated[nodeType] = true;
                
            } else if (nodeType == DIR_NODE) {
                
                float dirHeight = 1.04f;
                
                GL.NewList(displayList, ListMode.Compile); // start compiling display list
                //GL.Color3(boxColour[0], boxColour[1], boxColour[2]);
                //GL.Scale(BOX_SCALE, BOX_SCALE, BOX_SCALE);
                GL.Begin(BeginMode.Quads);          // start drawing quads
                
                // Front Face
                GL.Normal3( 0.0f, 0.0f, 1.0f);      // Normal Facing Forward
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(-0.8f, -0.8f,  0.8f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3( 0.8f, -0.8f,  0.8f);  // Bottom Left Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3( 0.8f,  dirHeight,  0.8f);  // Top Left Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(-0.8f,  dirHeight,  0.8f);  // Top Right Of The Texture and Quad
                // Back Face
                GL.Normal3( 0.0f, 0.0f,-1.0f);      // Normal Facing Away
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(-0.8f, -0.8f, -0.8f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(-0.8f,  dirHeight, -0.8f);  // Top Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3( 0.8f,  dirHeight, -0.8f);  // Top Left Of The Texture and Quad
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3( 0.8f, -0.8f, -0.8f);  // Bottom Left Of The Texture and Quad
                // Top Face
                GL.Normal3( 0.0f, 1.0f, 0.0f);      // Normal Facing Up
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(-0.8f,  dirHeight, -0.8f);  // Top Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(-0.8f,  dirHeight,  0.8f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3( 0.8f,  dirHeight,  0.8f);  // Bottom Left Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3( 0.8f,  dirHeight, -0.8f);  // Top Left Of The Texture and Quad
                // Bottom Face
                GL.Normal3( 0.0f,-1.0f, 0.0f);      // Normal Facing Down
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(-0.8f, -0.8f, -0.8f);  // Top Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3( 0.8f, -0.8f, -0.8f);  // Top Left Of The Texture and Quad
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3( 0.8f, -0.8f,  0.8f);  // Bottom Left Of The Texture and Quad
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(-0.8f, -0.8f,  0.8f);  // Bottom Right Of The Texture and Quad
                // Right face
                GL.Normal3( 1.0f, 0.0f, 0.0f);      // Normal Facing Right
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3( 0.8f, -0.8f, -0.8f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3( 0.8f,  dirHeight, -0.8f);  // Top Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3( 0.8f,  dirHeight,  0.8f);  // Top Left Of The Texture and Quad
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3( 0.8f, -0.8f,  0.8f);  // Bottom Left Of The Texture and Quad
                // Left Face
                GL.Normal3(-1.0f, 0.0f, 0.0f);      // Normal Facing Left
                GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(-0.8f, -0.8f, -0.8f);  // Bottom Left Of The Texture and Quad
                GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(-0.8f, -0.8f,  0.8f);  // Bottom Right Of The Texture and Quad
                GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(-0.8f,  dirHeight,  0.8f);  // Top Right Of The Texture and Quad
                GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(-0.8f,  dirHeight, -0.8f);  // Top Left Of The Texture and Quad
                
                GL.End();                    // Done Drawing Quads
                
                GL.EndList();                // Finish display list
                listGenerated[nodeType] = true;
                
            }
            
            
            
            return displayList;       
        }
        
        
        
         public static float[] GetColourForNode(FileNode node) {
           
            if(node.Description.Length > 0) {
                // step 1, calculate MD5 hash from input
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(COLOUR_SALT + node.Description);
                byte[] hash = md5.ComputeHash(inputBytes);
             
                // step 2, extract last 3 bytes from string
                float[] returnable = new float[3];
                returnable[0] = (((int)hash[hash.Length-1])/700.0f) + 0.3f;
                returnable[1] = (((int)hash[hash.Length-2])/700.0f) + 0.4f;
                returnable[2] = (((int)hash[hash.Length-3])/700.0f) + 0.4f;
                return returnable;         
                
            } else {
                float[] returnable = {0.2f, 0.6f, 0.6f};
                return returnable;
            }
            
        }
        
        public static DateTime ConvertFromUnixTimestamp(UInt64 timestamp) {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }
        
        private static float GetHeightForFolder(int numChildren) {
            if(numChildren > 200) {
                return 4.0f;
            } else if(numChildren > 100) {
                  return (3.0f + (1.0f * ((numChildren-100)/150.0f)));
            } else if(numChildren > 50) {
                return (2.5f + (0.5f * ((numChildren -50)/50.0f)));   
            } else if(numChildren > 25) {
                return (2.0f + (0.5f * ((numChildren - 25)/25.0f)));   
            } else if(numChildren > 10) {
                return (1.6f + (0.4f * ((numChildren - 10)/15.0f)));    
            } else if (numChildren > 5) {
                return (1.23f + (0.37f * ((numChildren - 5)/5.0f)));
            } else {
                return (1.0f + (0.23f * (numChildren/5.0f)));   
            }
        }
        
        
        
        
        
    }
}
