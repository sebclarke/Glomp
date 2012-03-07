using System;
// courtesy of Iain Craig, AF3DE


namespace glomp {

    public class FrustumCullerPlane
    {
        public float a;
        public float b;
        public float c;
        public float d;

        public FrustumCullerPlane ()
        {
            a=0;
            b=0;
            c=0;
            d=0;
        }

        ~FrustumCullerPlane ()
        {
            //Console.WriteLine ("FCP dying");
        }

        public float GetDistance(float x, float y, float z) 
        {
            // This function will simply perform a dot product of the point and
            // this plane.
            return a * x + b * y + c * z + d;
        }
    }

    public class FrustumCuller
    {
        FrustumCullerPlane[] Frustum;

        public FrustumCuller()
        {
            Frustum = new FrustumCullerPlane[6];
            for (int i=0; i<6; i++)
            {
                Frustum[i] = new FrustumCullerPlane();
            }
        }

        ~FrustumCuller ()
        {
            //Console.WriteLine ("FC dying");
        }

        public void CalculateFrustum (float[] md, float[] proj)
        {
            // Error checking.
            if(md == null || proj == null)
                return;

            // Create the clip.
            float[] clip = new float[16];
            for (int i=0; i<16; i++)
            {
                clip[i]=0;
            }

            clip[0] = md[0] * proj[0] + md[1] * proj[4] + md[2] * proj[8]  + md[3] * proj[12];
            clip[1] = md[0] * proj[1] + md[1] * proj[5] + md[2] * proj[9]  + md[3] * proj[13];
            clip[2] = md[0] * proj[2] + md[1] * proj[6] + md[2] * proj[10] + md[3] * proj[14];
            clip[3] = md[0] * proj[3] + md[1] * proj[7] + md[2] * proj[11] + md[3] * proj[15];

            clip[4] = md[4] * proj[0] + md[5] * proj[4] + md[6] * proj[8]  + md[7] * proj[12];
            clip[5] = md[4] * proj[1] + md[5] * proj[5] + md[6] * proj[9]  + md[7] * proj[13];
            clip[6] = md[4] * proj[2] + md[5] * proj[6] + md[6] * proj[10] + md[7] * proj[14];
            clip[7] = md[4] * proj[3] + md[5] * proj[7] + md[6] * proj[11] + md[7] * proj[15];

            clip[8]  = md[8] * proj[0] + md[9] * proj[4] + md[10] * proj[8]  + md[11] * proj[12];
            clip[9]  = md[8] * proj[1] + md[9] * proj[5] + md[10] * proj[9]  + md[11] * proj[13];
            clip[10] = md[8] * proj[2] + md[9] * proj[6] + md[10] * proj[10] + md[11] * proj[14];
            clip[11] = md[8] * proj[3] + md[9] * proj[7] + md[10] * proj[11] + md[11] * proj[15];

            clip[12] = md[12] * proj[0] + md[13] * proj[4] + md[14] * proj[8]  + md[15] * proj[12];
            clip[13] = md[12] * proj[1] + md[13] * proj[5] + md[14] * proj[9]  + md[15] * proj[13];
            clip[14] = md[12] * proj[2] + md[13] * proj[6] + md[14] * proj[10] + md[15] * proj[14];
            clip[15] = md[12] * proj[3] + md[13] * proj[7] + md[14] * proj[11] + md[15] * proj[15];
            

            // Calculate the right side of the frustum.
            Frustum[0].a = clip[3]  - clip[0];
            Frustum[0].b = clip[7]  - clip[4];
            Frustum[0].c = clip[11] - clip[8];
            Frustum[0].d = clip[15] - clip[12];

            // Calculate the left side of the frustum.
            Frustum[1].a = clip[3]  + clip[0];
            Frustum[1].b = clip[7]  + clip[4];
            Frustum[1].c = clip[11] + clip[8];
            Frustum[1].d = clip[15] + clip[12];

            // Calculate the bottom side of the frustum.
            Frustum[2].a = clip[3]  + clip[1];
            Frustum[2].b = clip[7]  + clip[5];
            Frustum[2].c = clip[11] + clip[9];
            Frustum[2].d = clip[15] + clip[13];

            // Calculate the top side of the frustum.
            Frustum[3].a = clip[3]  - clip[1];
            Frustum[3].b = clip[7]  - clip[5];
            Frustum[3].c = clip[11] - clip[9];
            Frustum[3].d = clip[15] - clip[13];

            // Calculate the far side of the frustum.
            Frustum[4].a = clip[3]  - clip[2];
            Frustum[4].b = clip[7]  - clip[6];
            Frustum[4].c = clip[11] - clip[10];
            Frustum[4].d = clip[15] - clip[14];

            // Calculate the near side of the frustum.
            Frustum[5].a = clip[3]  + clip[2];
            Frustum[5].b = clip[7]  + clip[6];
            Frustum[5].c = clip[11] + clip[10];
            Frustum[5].d = clip[15] + clip[14];

            // Normalize the sides of the frustum.
            NormalizeFrustum();
        }

        private void NormalizeFrustum()
        {
            float magnitude = 0.0f;

            // Loop through each side of the frustum and normalize it.
            for(int i = 0; i < 6; i++)
            {
                magnitude = (float)Math.Sqrt(Frustum[i].a * Frustum[i].a + 
                                        Frustum[i].b * Frustum[i].b + 
                                            Frustum[i].c * Frustum[i].c);
                magnitude = 1 / magnitude;

                Frustum[i].a *= magnitude;
                Frustum[i].b *= magnitude;
                Frustum[i].c *= magnitude;
                Frustum[i].d *= magnitude;
            }
        }

        public bool isPointVisiable(float x, float y, float z)
        {
            // Loop through each side of the frustum and test if the point lies outside any of them.
            for(int i = 0; i < 6; i++)
            {
                if(Frustum[i].GetDistance(x, y, z) < 0)
                    return false;
            }

            return true;
        }

        public bool isSphereVisiable(float x, float y, float z, float radius)
        {
            float distance = 0;

            // Loop through each side of the frustum and test if the sphere lies outside any of them.
            for(int i = 0; i < 6; i++)
                {
                    distance = Frustum[i].GetDistance(x, y, z);

                    if(distance < -radius)
                        return false;
                }

            return true;
        }


        public bool isBoxVisible(float x, float y, float z, float baseRadius, float height)
        {
            float minX, maxX;
            float minY, maxY;
            float minZ, maxZ;

            // Calculate the bounding box.
            minX = x - baseRadius; maxX = x + baseRadius;
            minY = y; maxY = y + height;
            minZ = z - baseRadius; maxZ = z + baseRadius;
               
            // Loop through each side of the frustum and test if the box lies outside any of them.
            for(int i = 0; i < 6; i++)
                {
                    if(Frustum[i].GetDistance(minX, minY, minZ) > 0) continue;
                    if(Frustum[i].GetDistance(maxX, minY, minZ) > 0) continue;   
                    if(Frustum[i].GetDistance(minX, maxY, minZ) > 0) continue;   
                    if(Frustum[i].GetDistance(maxX, maxY, minZ) > 0) continue;   
                    if(Frustum[i].GetDistance(minX, minY, maxZ) > 0) continue;   
                    if(Frustum[i].GetDistance(maxX, minY, maxZ) > 0) continue;   
                    if(Frustum[i].GetDistance(minX, maxY, maxZ) > 0) continue;   
                    if(Frustum[i].GetDistance(maxX, maxY, maxZ) > 0) continue;
                     
                    return false;
                }

            return true;
        }
    }
}