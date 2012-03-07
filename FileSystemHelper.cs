
using System;
using System.Collections.Generic;
using System.IO;
using OpenTK;


namespace glomp {


    public class FileSystemHelper {

        public FileSystemHelper() {
        }
        
        public static LinkedList<SceneNode> GetSceneFromPath(String path) {
            // set up offsets for node positioning
            float xOffset, startX, yOffset, zOffset;
            xOffset = startX = 6.0f;
            yOffset = -2.0f;
            zOffset = 10.0f;
            
            
            int nodeCount = 0;
            float spacing = 6.0f;
            
            String[] files = Directory.GetFiles(path);
            int gridWidth = (int)Math.Round(Math.Sqrt(files.Length)/1.6f, 0);
            
            LinkedList<TextNode> textNodeList = new LinkedList<TextNode>();
            LinkedList<SceneNode> sceneList = new LinkedList<SceneNode>();
            
            foreach(var file in files) {
                float xPosition = xOffset - ((nodeCount % gridWidth) * spacing);
                float zPosition = zOffset + ((nodeCount / gridWidth) * spacing);
                BoxNode box = new BoxNode(new Vector3(xPosition, yOffset, zPosition), 0.8f);
                box.Build();
                sceneList.AddLast(box);
                
                TextNode text = new TextNode(file.Replace(path, ""));
                text.Build();
                text.Position = box.Position;
                text.OriginOffset = new Vector3(-3.1f, 0f, 0f);
                textNodeList.AddLast(text);
                nodeCount++;
                System.Console.WriteLine(file);
            }
            
            foreach(var textNode in textNodeList) {
                sceneList.AddLast(textNode);
            }
            
            return sceneList;
        }
    }
}
