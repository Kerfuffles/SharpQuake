/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

// cdaudio.h

namespace SharpQuake
{
    /// <summary>
    /// CDAudio_functions
    /// </summary>
    internal static class cd_audio
    {
#if _WINDOWS
        private static ICDAudioController _Controller = new CDAudioWinController();
#else
        static ICDAudioController _Controller = new NullCDAudioController();
#endif

        /// <summary>
        /// CDAudio_Init
        /// </summary>
        public static bool Init()
        {
            if( client.cls.state == cactive_t.ca_dedicated )
                return false;

            if( common.HasParam( "-nocdaudio" ) )
                return false;

            _Controller.Init();

            if( _Controller.IsInitialized )
            {
                cmd.Add( "cd", CD_f );
                Con.Print( "CD Audio Initialized\n" );
            }

            return _Controller.IsInitialized;
        }

        // CDAudio_Play(byte track, qboolean looping)
        public static void Play( byte track, bool looping )
        {
            _Controller.Play( track, looping );
        }

        // CDAudio_Stop
        public static void Stop()
        {
            _Controller.Stop();
        }

        // CDAudio_Pause
        public static void Pause()
        {
            _Controller.Pause();
        }

        // CDAudio_Resume
        public static void Resume()
        {
            _Controller.Resume();
        }

        // CDAudio_Shutdown
        public static void Shutdown()
        {
            _Controller.Shutdown();
        }

        // CDAudio_Update
        public static void Update()
        {
            _Controller.Update();
        }

        private static void CD_f()
        {
            if( cmd.Argc < 2 )
                return;

            string command = cmd.Argv( 1 );

            if( common.SameText( command, "on" ) )
            {
                _Controller.IsEnabled = true;
                return;
            }

            if( common.SameText( command, "off" ) )
            {
                if( _Controller.IsPlaying )
                    _Controller.Stop();
                _Controller.IsEnabled = false;
                return;
            }

            if( common.SameText( command, "reset" ) )
            {
                _Controller.IsEnabled = true;
                if( _Controller.IsPlaying )
                    _Controller.Stop();

                _Controller.ReloadDiskInfo();
                return;
            }

            if( common.SameText( command, "remap" ) )
            {
                int ret = cmd.Argc - 2;
                byte[] remap = _Controller.Remap;
                if( ret <= 0 )
                {
                    for( int n = 1; n < 100; n++ )
                        if( remap[n] != n )
                            Con.Print( "  {0} -> {1}\n", n, remap[n] );
                    return;
                }
                for( int n = 1; n <= ret; n++ )
                    remap[n] = (byte)common.atoi( cmd.Argv( n + 1 ) );
                return;
            }

            if( common.SameText( command, "close" ) )
            {
                _Controller.CloseDoor();
                return;
            }

            if( !_Controller.IsValidCD )
            {
                _Controller.ReloadDiskInfo();
                if( !_Controller.IsValidCD )
                {
                    Con.Print( "No CD in player.\n" );
                    return;
                }
            }

            if( common.SameText( command, "play" ) )
            {
                _Controller.Play( (byte)common.atoi( cmd.Argv( 2 ) ), false );
                return;
            }

            if( common.SameText( command, "loop" ) )
            {
                _Controller.Play( (byte)common.atoi( cmd.Argv( 2 ) ), true );
                return;
            }

            if( common.SameText( command, "stop" ) )
            {
                _Controller.Stop();
                return;
            }

            if( common.SameText( command, "pause" ) )
            {
                _Controller.Pause();
                return;
            }

            if( common.SameText( command, "resume" ) )
            {
                _Controller.Resume();
                return;
            }

            if( common.SameText( command, "eject" ) )
            {
                if( _Controller.IsPlaying )
                    _Controller.Stop();
                _Controller.Edject();
                return;
            }

            if( common.SameText( command, "info" ) )
            {
                Con.Print( "%u tracks\n", _Controller.MaxTrack );
                if( _Controller.IsPlaying )
                    Con.Print( "Currently {0} track {1}\n", _Controller.IsLooping ? "looping" : "playing", _Controller.CurrentTrack );
                else if( _Controller.IsPaused )
                    Con.Print( "Paused {0} track {1}\n", _Controller.IsLooping ? "looping" : "playing", _Controller.CurrentTrack );
                Con.Print( "Volume is {0}\n", _Controller.Volume );
                return;
            }
        }
    }

    internal class NullCDAudioController : ICDAudioController
    {
        private byte[] _Remap;

        public NullCDAudioController()
        {
            _Remap = new byte[100];
        }

        #region ICDAudioController Members

        public bool IsInitialized
        {
            get
            {
                return true;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public bool IsPlaying
        {
            get
            {
                return false;
            }
        }

        public bool IsPaused
        {
            get
            {
                return false;
            }
        }

        public bool IsValidCD
        {
            get
            {
                return false;
            }
        }

        public bool IsLooping
        {
            get
            {
                return false;
            }
        }

        public byte[] Remap
        {
            get
            {
                return _Remap;
            }
        }

        public byte MaxTrack
        {
            get
            {
                return 0;
            }
        }

        public byte CurrentTrack
        {
            get
            {
                return 0;
            }
        }

        public float Volume
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public void Init()
        {
        }

        public void Play( byte track, bool looping )
        {
        }

        public void Stop()
        {
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Shutdown()
        {
        }

        public void Update()
        {
        }

        public void ReloadDiskInfo()
        {
        }

        public void CloseDoor()
        {
        }

        public void Edject()
        {
        }

        #endregion ICDAudioController Members
    }

    internal interface ICDAudioController
    {
        bool IsInitialized
        {
            get;
        }

        bool IsEnabled
        {
            get; set;
        }

        bool IsPlaying
        {
            get;
        }

        bool IsPaused
        {
            get;
        }

        bool IsValidCD
        {
            get;
        }

        bool IsLooping
        {
            get;
        }

        byte[] Remap
        {
            get;
        }

        byte MaxTrack
        {
            get;
        }

        byte CurrentTrack
        {
            get;
        }

        float Volume
        {
            get; set;
        }

        void Init();

        void Play( byte track, bool looping );

        void Stop();

        void Pause();

        void Resume();

        void Shutdown();

        void Update();

        void ReloadDiskInfo();

        void CloseDoor();

        void Edject();
    }
}