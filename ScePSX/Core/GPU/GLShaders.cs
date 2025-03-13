
using System;
using System.Text;
using OpenGL;

namespace ScePSX
{
    public class GlShader
    {
        public uint Program;

        public GlShader()
        {
            Load(
                ShaderStrings.VertixShader.Split(new string[] { "\r" }, StringSplitOptions.None),
                ShaderStrings.FragmentShader.Split(new string[] { "\r" }, StringSplitOptions.None)
                );
        }

        public void Load(string[] vert, string[] frag)
        {
            uint vertexShader = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vertexShader, vert);
            CompileShader(vertexShader);

            uint fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
            Gl.ShaderSource(fragmentShader, frag);
            CompileShader(fragmentShader);

            Program = Gl.CreateProgram();
            Gl.AttachShader(Program, vertexShader);
            Gl.AttachShader(Program, fragmentShader);

            Gl.LinkProgram(Program);
            Gl.GetProgram(Program, ProgramProperty.LinkStatus, out var code);
            if (code != 1)
            {
                throw new Exception($"Error occurred whilst linking Program({Program})");
            }

            Gl.DetachShader(Program, vertexShader);
            Gl.DetachShader(Program, fragmentShader);
            Gl.DeleteShader(fragmentShader);
            Gl.DeleteShader(vertexShader);

            Console.WriteLine($"[OPENGL GPU] LinkProgram Done!");
        }

        private void CompileShader(uint shader)
        {
            Gl.CompileShader(shader);

            Gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
            {
                Gl.GetShader(shader, ShaderParameterName.InfoLogLength, out int logLength);
                if (logLength <= 0)
                    return;

                StringBuilder infoLog = new StringBuilder(logLength);
                Gl.GetShaderInfoLog(shader, logLength, out int actualLength, infoLog);

                string log = infoLog.ToString();
                Console.WriteLine($"[OPENGL GPU] Shader compile error:\r\n{log}\r\n");
            }
        }

        public void Use()
        {
            Gl.UseProgram(Program);
        }
    }

    public static class ShaderStrings
    {

        public static string VertixShader = @"
            #version 330 

            layout(location = 0) in ivec2 vertixInput;
            layout(location = 1) in uvec3 vColors;
            layout(location = 2) in vec2 inUV;


            out vec3 color_in;
            out vec2 texCoords;
            flat out ivec2 clutBase;
            flat out ivec2 texpageBase;
         
            uniform int renderMode = 0;
            flat out int renderModeFrag;

            uniform int inClut;
            uniform int inTexpage;

            uniform float display_area_x_start = 0.0f;
            uniform float display_area_y_start = 0.0f;

            uniform float display_area_x_end = 1.0f;
            uniform float display_area_y_end = 1.0f;

            uniform float aspect_ratio_x_offset = 0.0;
            uniform float aspect_ratio_y_offset = 0.0;

            void main()
            {
    
            //Convert x from [0,1023] and y from [0,511] coords to [-1,1]

            float xpos = ((float(vertixInput.x) + 0.5) / 512.0) - 1.0;
            float ypos = ((float(vertixInput.y) - 0.5) / 256.0) - 1.0;

            //float xpos = ((float(vertixInput.x) / 1024.0) * 2.0) - 1.0;
            //float ypos = ((float(vertixInput.y) / 512.0) * 2.0) - 1.0;

	        vec4 positions[4];
            vec2 texcoords[4];
            renderModeFrag = renderMode;
            
            //TODO: Clean up 

            switch(renderMode){
                 case 0:            
                        gl_Position.xyzw = vec4(xpos,ypos,0.0, 1.0); 
                        texpageBase = ivec2((inTexpage & 0xf) * 64, ((inTexpage >> 4) & 0x1) * 256);
                        clutBase = ivec2((inClut & 0x3f) * 16, inClut >> 6);
                        texCoords = inUV;

                        color_in = vec3(
                        float(vColors.r)/255.0,
                        float(vColors.g)/255.0,
                        float(vColors.b)/255.0);

                        return;

                 case 1:         //16/24bpp vram -> Screen
                 case 2:         
                        positions = vec4[](
                        vec4(-1.0 + aspect_ratio_x_offset, 1.0 - aspect_ratio_y_offset, 1.0, 1.0),    // Top-left
                        vec4(1.0 - aspect_ratio_x_offset, 1.0 - aspect_ratio_y_offset, 1.0, 1.0),     // Top-right
                        vec4(-1.0 + aspect_ratio_x_offset, -1.0 + aspect_ratio_y_offset, 1.0, 1.0),   // Bottom-left
                        vec4(1.0 - aspect_ratio_x_offset, -1.0 + aspect_ratio_y_offset, 1.0, 1.0));   // Bottom-right

                        texcoords = vec2[](		//Inverted in Y because PS1 Y coords are inverted
                        vec2(display_area_x_start, display_area_y_start),   			    // Top-left
                        vec2(display_area_x_end, display_area_y_start),                     // Top-right
                        vec2(display_area_x_start, display_area_y_end),                     // Bottom-left
                        vec2(display_area_x_end, display_area_y_end));                      // Bottom-right

                        break;
            }

          
            texCoords = texcoords[gl_VertexID];
            gl_Position = positions[gl_VertexID];

            return;

              
            }";

        public static string FragmentShader = @"
            #version 330 

            in vec3 color_in;
            in vec2 texCoords;
            flat in ivec2 clutBase;
            flat in ivec2 texpageBase;

            uniform int TextureMode;

            uniform int isDithered;
            uniform int transparencyMode;                   //4 = disabled
            uniform int maskBitSetting;
            uniform int isCopy = 0;                         //Only change when doing copy by render

            flat in int renderModeFrag;

            uniform ivec4 u_texWindow;

            uniform sampler2D u_vramTex;

            //out vec4 outputColor;
            layout(location = 0, index = 0) out vec4 outputColor;
            layout(location = 0, index = 1) out vec4 outputBlendColor;

         
           mat4 ditheringTable = mat4(
               -4,  0, -3,  1,
                2, -2,  3, -1,
               -3,  1, -4,  0,
                3, -1,  2, -2
            );

            vec3 dither(vec3 colors, vec2 position) {

               // % 4
               int x = int((position.x * 1023.0)) & 3;
               int y = int((position.y * 511.0)) & 3;

               int ditherOffset = int(ditheringTable[y][x]);

               colors = colors * vec3(255.0, 255.0, 255.0);
               colors = colors + vec3(ditherOffset, ditherOffset ,ditherOffset);

               //Clamping to [0,255] (or [0,1]) is automatically done because 
               //the frame buffer format is of a normalized fixed-point (RGB5A1)

              return colors / vec3(255.0, 255.0, 255.0);

              }

            vec4 grayScale(vec4 color) {
                   float x = 0.299*(color.r) + 0.587*(color.g) + 0.114*(color.b);
                   return vec4(x,x,x,1);
              }

               int floatToU5(float f) {				
                        return int(floor(f * 31.0 + 0.5));
                  }

               int floatToU8(float f) {				
                        return int(floor(f * 255.0 + 0.5));
                  }

            vec4 sampleVRAM(ivec2 coords) {
                   coords &= ivec2(1023, 511); // Out-of-bounds VRAM accesses wrap
                   return texelFetch(u_vramTex, coords, 0);
              }

            int sample16(ivec2 coords) {
                   vec4 colour = sampleVRAM(coords);
                   int r = floatToU5(colour.r);
                   int g = floatToU5(colour.g);
                   int b = floatToU5(colour.b);
                   int msb = int(ceil(colour.a)) << 15;
                   return r | (g << 5) | (b << 10) | msb;
                }

             vec4 texBlend(vec4 colour1, vec4 colour2) {
                        vec4 ret = (colour1 * colour2) / (128.0 / 255.0);
                        ret.a = 1.0;
                        return ret;
                }

              vec4 handleAlphaValues() {
                        vec4 alpha;

                        switch (transparencyMode) {
                            case 0:                     // B/2 + F/2
                                alpha.xyzw = vec4(0.5, 0.5, 0.5, 0.5);      
                                break;

                            case 1:                     // B + F
                            case 2:                     // B - F (Function will change to reverse subtract)
                                alpha.xyzw = vec4(1.0, 1.0, 1.0, 1.0);      
                                break;

                            case 3:                     // B + F/4
                                alpha.xyzw = vec4(0.25, 0.25, 0.25, 1.0);      
                                break; 
                              }

                        return alpha;

                    }

            vec4 handle24bpp(ivec2 coords){

                 //Each 6 bytes (3 shorts) contain two 24bit pixels.
                 //Step 1.5 short for each x since 1 24bits = 3/2 shorts 

                 int xx = ((coords.x << 1) + coords.x) >> 1; //xx = int(coords.x * 1.5)

                 if(xx > 1022 || coords.y > 511){ return vec4(0.0f, 0.0f, 0.0f, 0.0f); }  //Ignore reading out of vram
                    
                 int p0 = sample16(ivec2(xx, coords.y));
                 int p1 = sample16(ivec2(xx + 1, coords.y));
                 
                 vec4 color; 
                 if ((coords.x & 1) != 0) {         
                     color.r = (p0 >> 8) & 0xFF;
                     color.g = p1 & 0xFF;
                     color.b = (p1 >> 8) & 0xFF;
                 } else {
                     color.r = p0 & 0xFF;
                     color.g = (p0 >> 8) & 0xFF;
                     color.b = (p1 & 0xFF);
                 } 

                return color / vec4(255.0f, 255.0f, 255.0f, 255.0f);   
            }

            void main()
            {

                ivec2 coords;

                switch(renderModeFrag){
                    case 0: break;

                    case 1: //As 16bpp
                            coords = ivec2(texCoords * vec2(1024.0, 512.0)); 
                            outputColor.rgba = sampleVRAM(coords);
                            return;

                    case 2: //As 24bpp
                            coords = ivec2(texCoords * vec2(1024.0, 512.0)); 
                            outputColor.rgba = handle24bpp(coords);
                            return;
                }


                //Fix up UVs and apply texture window
                  ivec2 UV = ivec2(floor(texCoords + vec2(0.0001, 0.0001))) & ivec2(0xFF);
                  UV = (UV & ~(u_texWindow.xy * 8)) | ((u_texWindow.xy & u_texWindow.zw) * 8); //XY contain Mask, ZW contain Offset  


  	            if(TextureMode == -1){		//No texture, for now i am using my own flag (TextureMode) instead of (inTexpage & 0x8000) 
    		             outputColor.rgb = vec3(color_in.r, color_in.g, color_in.b);
                         outputBlendColor = handleAlphaValues();
                            if((maskBitSetting & 1) == 1){
                                outputColor.a = 1.0;
                            }else{
                                outputColor.a = 0.0;
                            }
                            
                         if(((maskBitSetting >> 1) & 1) == 1){
                            int currentPixel = sample16(ivec2((gl_FragCoord.xy - vec2(0.5,0.5)) * vec2(1024.0, 512.0)));
                            if(((currentPixel >> 15) & 1) == 1){
                                discard;
                            }
                         }

                 }else if(TextureMode == 0){  //4 Bit texture
 		               ivec2 texelCoord = ivec2(UV.x >> 2, UV.y) + texpageBase;
               
       	               int sample = sample16(texelCoord);
                       int shift = (UV.x & 3) << 2;
                       int clutIndex = (sample >> shift) & 0xf;

                       ivec2 sampleCoords = ivec2(clutBase.x + clutIndex, clutBase.y);

                       outputColor = texelFetch(u_vramTex, sampleCoords, 0);

		                if (outputColor.rgba == vec4(0.0, 0.0, 0.0, 0.0) || 
                            ((outputColor.rgba == vec4(0.0, 0.0, 0.0, 1.0)) && (transparencyMode != 4))) { discard; }

                        outputColor = texBlend(outputColor, vec4(color_in,1.0));

                        //Check if pixel is transparent depending on bit 15 of the final color value

                        bool isTransparent = (((sample16(sampleCoords) >> 15) & 1) == 1);     

                        if(isTransparent && transparencyMode != 4){
                           outputBlendColor = handleAlphaValues();
                        }else{
                          outputBlendColor = vec4(1.0, 1.0, 1.0, 0.0);
                        }

                        //Handle Mask Bit setting

		                  if((maskBitSetting & 1) == 1){
                                outputColor.a = 1.0;
                            }

                            if(((maskBitSetting >> 1) & 1) == 1){
                                int currentPixel = sample16(ivec2((gl_FragCoord.xy - vec2(0.5,0.5)) * vec2(1024.0, 512.0)));
                                if(((currentPixel >> 15) & 1) == 1){
                                    discard;
                                }
                            } 


	            }else if (TextureMode == 1) { // 8 bit texture

                           ivec2 texelCoord = ivec2(UV.x >> 1, UV.y) + texpageBase;
               
                           int sample = sample16(texelCoord);
                           int shift = (UV.x & 1) << 3;
                           int clutIndex = (sample >> shift) & 0xff;
                           ivec2 sampleCoords = ivec2(clutBase.x + clutIndex, clutBase.y);
                           outputColor = texelFetch(u_vramTex, sampleCoords, 0);

                            if (outputColor.rgba == vec4(0.0, 0.0, 0.0, 0.0) || 
                            ((outputColor.rgba == vec4(0.0, 0.0, 0.0, 1.0)) && (transparencyMode != 4))) { discard; }

                           outputColor = texBlend(outputColor, vec4(color_in,1.0));

                           //Check if pixel is transparent depending on bit 15 of the final color value

                            bool isTransparent = (((sample16(sampleCoords) >> 15) & 1) == 1);     
                        
                            if(isTransparent && transparencyMode != 4){
                                 outputBlendColor = handleAlphaValues();

                            }else{
                                outputBlendColor = vec4(1.0, 1.0, 1.0, 0.0);
                            }

                        
                            //Handle Mask Bit setting
                            if((maskBitSetting & 1) == 1){
                                outputColor.a = 1.0;
                             }

                            if(((maskBitSetting >> 1) & 1) == 1){
                                int currentPixel = sample16(ivec2((gl_FragCoord.xy - vec2(0.5,0.5)) * vec2(1024.0, 512.0)));
                                if(((currentPixel >> 15) & 1) == 1){
                                    discard;
                                }
                            }

                    }

	            else {  //16 Bit texture

                        
                       if(isCopy == 0){ 
                                ivec2 texelCoord = UV + texpageBase;
                                outputColor = sampleVRAM(texelCoord);
                    
                                if (outputColor.rgba == vec4(0.0, 0.0, 0.0, 0.0) || 
                                ((outputColor.rgba == vec4(0.0, 0.0, 0.0, 1.0)) && (transparencyMode != 4))) { discard; }

                                outputColor = texBlend(outputColor, vec4(color_in,1.0));	
                            
                                //Check if pixel is transparent depending on bit 15 of the final color value

                                 bool isTransparent = (((sample16(texelCoord) >> 15) & 1) == 1);     

                                 if(isTransparent && transparencyMode != 4){
                                     outputBlendColor = handleAlphaValues();

                                }else{
                                     outputBlendColor  = vec4(1.0, 1.0, 1.0, 0.0);
                                }
                                 
                        } else {
                                outputColor = sampleVRAM(ivec2(texCoords)); 
                                outputBlendColor  = vec4(1.0, 1.0, 1.0, 0.0);
                        }

 		               
                        //Handle Mask Bit setting (affects both render and copy commands)

                        if((maskBitSetting & 1) == 1){
                                outputColor.a = 1.0;
                        }      

                        if(((maskBitSetting >> 1) & 1) == 1){
                                int currentPixel = sample16(ivec2((gl_FragCoord.xy - vec2(0.5,0.5)) * vec2(1024.0, 512.0)));
                                if(((currentPixel >> 15) & 1) == 1){
                                    discard;
                                }
                        } 
                                    
	                }

                    //Dithering is the same for all modes 
                    if(isDithered == 1){    
                         outputColor.rgb = dither(outputColor.rgb, gl_FragCoord.xy - vec2(0.5, 0.5));
                    }
        
            }";

    }
}
