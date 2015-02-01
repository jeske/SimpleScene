using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;

using SimpleScene;

namespace Example2DTileGame
{
    partial class Example2DTileGame : OpenTK.GameWindow
    {

        bool canMove = true; // Help detect collision
        /// <summary>
        /// Move the camera - needs work
        /// </summary>
        public void moveCamera()
        {

            // Follow player
           // camera.basePos.Xz = player.Pos.Xz;         

        }
        
        /// <summary>
        /// Move the player
        /// </summary>
        public void movePlayer()
        {

            // Still a work in progress - but it's getting somewhere
            player = getPlayer();
            // TODO - distinguish each condition so they don't all evaluate to true at once
            if (camera.Dir.X >= 0 && Keyboard[Key.W] && canMove && player.Dir.X == 0
                && player.Dir.X != 1)
            {
                PlayerX--;
                Console.WriteLine(player.Dir.Z);
                player.Pos = new Vector3(PlayerX, PlayerY, PlayerZ);
            }

            else if (camera.Dir.X <= 0 && Keyboard[Key.W] && canMove && player.Dir.X == 0)
            {
                PlayerX++;
             
                player.Pos = new Vector3(PlayerX, PlayerY, PlayerZ);
            }

            else if (camera.Dir.Z >= 0 && Keyboard[Key.W] && canMove && player.Dir.Z == 0)
            {
                PlayerZ--;
                Console.WriteLine(player.Dir.Z);
                player.Pos = new Vector3(PlayerX, PlayerY, PlayerZ);
            }

            else if (camera.Dir.Z <= 0 && Keyboard[Key.W] && canMove && player.Dir.Z == 1)
            {
                PlayerZ++;
                Console.WriteLine(player.Dir.Z);
                player.Pos = new Vector3(PlayerX, PlayerY, PlayerZ);
            }
        }

        /// <summary>
        /// Detect if there is collision
        /// </summary>
        public void collide()
        {
            if (player.Pos.X > map.GetLength(0) * 2)
            {
                canMove = false;
                Console.WriteLine("Collide");
            }
        }
    }
}
