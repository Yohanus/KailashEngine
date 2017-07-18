using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK.Graphics.OpenGL;

using MuffinEngine.Render.Shader;
using MuffinEngine.Render.Objects;
using MuffinEngine.Output;

namespace MuffinEngine.Render.FX
{
    class fx_Test : RenderEffect
    {

        // Programs


        // Frame Buffers

        
        // Textures





        public fx_Test(ProgramLoader pLoader, string glsl_effect_path, Resolution full_resolution)
            : base(pLoader, glsl_effect_path, full_resolution)
        { }

        protected override void load_Programs()
        {

        }

        protected override void load_Buffers()
        {

        }

        public override void load()
        {
            load_Programs();
            load_Buffers();
        }

        public override void unload()
        {

        }

        public override void reload()
        {

        }


        public void render(fx_Quad quad)
        {
            
        }


    }
}
