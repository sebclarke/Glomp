
using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace glomp {


    public class Camera {

        private Vector3 position;
        private float pitch;
        private float yaw;
        private Vector3 lastMove;
        
        public Vector3 Position {
            get { return position; }
        }
        
        public Vector3 LastMove {
            get { return lastMove; }
        }
        
        public Camera() {
            position = Vector3.Zero;
            pitch = 0.0f;
            yaw = 0.0f;
        }
        
        
        public void Put(Vector3 newPosition) {
            position = newPosition;
          
        }
        public void Put(Vector3 newPosition, float newPitch, float newYaw) {
            position = newPosition;
            pitch = newPitch;
            yaw = newYaw;  
        }
        
        public void Move(Vector3 moveVector) {
            position += moveVector;
            lastMove = moveVector;
        }
        
        public void adjustYaw(float amount) {
            yaw += amount;
        }
        
        public void adjustPitch(float amount) {
            pitch += amount;
        }
        
        public void Transform() {
            // set up transforms which are inverse of our position
            // ie bring the world to meet the camera rather than move the camera
            Vector3 translate = -position;
            float rotY = 360.0f - yaw;
            float rotX = 360.0f - pitch;
            
            GL.Rotate(rotX, Vector3.UnitX);
            GL.Rotate(rotY, Vector3.UnitY);
            GL.Translate(translate);
        }
        
    }
}
