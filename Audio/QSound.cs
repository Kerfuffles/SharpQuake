/* Rewritten in C# by Yury Kiselev, 2010.
 *
 * Copyright (C) 1996-1997 Id Software, Inc.
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using System;
using OpenTK;

namespace SharpQuake
{
    /// <summary>
    /// S_functions
    /// </summary>
    static partial class QSound
    {
        public static bool IsInitialized { get { return _Controller.IsInitialized; } }

        public static QSoundData shm { get { return _shm; } }

        public static float BgmVolume { get { return _BgmVolume.Value; } }

        public static float Volume { get { return _Volume.Value; } }

        public const int   DEFAULT_SOUND_PACKET_VOLUME      = 255;
        public const float DEFAULT_SOUND_PACKET_ATTENUATION = 1.0f;
        public const int   MAX_CHANNELS                     = 128;
        public const int   MAX_DYNAMIC_CHANNELS             = 8;

        private const int MAX_SFX = 512;

        private static cvar _BgmVolume     = new cvar( "bgmvolume",         "1",   true ); // = { "bgmvolume", "1", true };
        private static cvar _Volume        = new cvar( "volume",            "0.7", true ); // = { "volume", "0.7", true };
        private static cvar _NoSound       = new cvar( "nosound",           "0" );         // = { "nosound", "0" };
        private static cvar _Precache      = new cvar( "precache",          "1" );         // = { "precache", "1" };
        private static cvar _LoadAs8bit    = new cvar( "loadas8bit",        "0" );         // = { "loadas8bit", "0" };
        private static cvar _BgmBuffer     = new cvar( "bgmbuffer",         "4096" );      // = { "bgmbuffer", "4096" };
        private static cvar _AmbientLevel  = new cvar( "ambient_level",     "0.3" );       // = { "ambient_level", "0.3" };
        private static cvar _AmbientFade   = new cvar( "ambient_fade",      "100" );       // = { "ambient_fade", "100" };
        private static cvar _NoExtraUpdate = new cvar( "snd_noextraupdate", "0" );         // = { "snd_noextraupdate", "0" };
        private static cvar _Show          = new cvar( "snd_show",          "0" );         // = { "snd_show", "0" };
        private static cvar _MixAhead      = new cvar( "_snd_mixahead",     "0.1", true ); // = { "_snd_mixahead", "0.1", true };

        private static ISoundController _Controller = new OpenALController(); // NullSoundController();
        private static bool             _IsInitialized;                       // snd_initialized

        private static QSoundFX[] _KnownSfx = new QSoundFX[MAX_SFX];                       // hunk allocated [MAX_SFX]
        private static int        _NumSfx;                                                 // num_sfx
        private static QSoundFX[] _AmbientSfx = new QSoundFX[QBSPAmbientFlag.NUM_AMBIENTS]; // *ambient_sfx[NUM_AMBIENTS]
        private static bool       _Ambient    = true;                                      // snd_ambient
        private static QSoundData _shm        = new QSoundData();                          // shm

        // 0 to MAX_DYNAMIC_CHANNELS-1	= normal entity sounds
        // MAX_DYNAMIC_CHANNELS to MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS -1 = water, etc
        // MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS to total_channels = static sounds
        private static QSoundChannel[] _Channels = new QSoundChannel[MAX_CHANNELS]; // channels[MAX_CHANNELS]

        private static int _TotalChannels; // total_channels

        private static float   _SoundNominalClipDist = 1000.0f; // sound_nominal_clip_dist
        private static Vector3 _ListenerOrigin;                 // listener_origin
        private static Vector3 _ListenerForward;                // listener_forward
        private static Vector3 _ListenerRight;                  // listener_right
        private static Vector3 _ListenerUp;                     // listener_up

        private static int  _SoundTime;         // soundtime		// sample PAIRS
        private static int  _PaintedTime;       // paintedtime 	// sample PAIRS
        private static bool _SoundStarted;      // sound_started
        private static int  _SoundBlocked = 0;  // snd_blocked
        private static int  _OldSamplePos;      // oldsamplepos from GetSoundTime()
        private static int  _Buffers;           // buffers from GetSoundTime()
        private static int  _PlayHash    = 345; // hash from S_Play()
        private static int  _PlayVolHash = 543; // hash S_PlayVol

        // S_Init (void)
        public static void Init()
        {
            Con.Print( "\nSound Initialization\n" );

            if( QCommon.HasParam( "-nosound" ) )
                return;

            for( int i = 0; i < _Channels.Length; i++ )
                _Channels[i] = new QSoundChannel();

            QCommand.Add( "play",      Play );
            QCommand.Add( "playvol",   PlayVol );
            QCommand.Add( "stopsound", StopAllSoundsCmd );
            QCommand.Add( "soundlist", SoundList );
            QCommand.Add( "soundinfo", SoundInfo_f );

            _IsInitialized = true;

            Startup();

            InitScaletable();

            _NumSfx = 0;

            Con.Print( "Sound sampling rate: {0}\n", _shm.speed );

            // provides a tick sound until washed clean
            _AmbientSfx[QBSPAmbientFlag.AMBIENT_WATER] = PrecacheSound( "ambience/water1.wav" );
            _AmbientSfx[QBSPAmbientFlag.AMBIENT_SKY]   = PrecacheSound( "ambience/wind2.wav" );

            StopAllSounds( true );
        }

        // S_AmbientOff (void)
        public static void AmbientOff()
        {
            _Ambient = false;
        }

        // S_AmbientOn (void)
        public static void AmbientOn()
        {
            _Ambient = true;
        }

        // S_Shutdown (void)
        public static void Shutdown()
        {
            if( !_Controller.IsInitialized )
                return;

            if( _shm != null )
                _shm.gamealive = false;

            _Controller.Shutdown();
            _shm = null;
        }

        // S_TouchSound (char *sample)
        public static void TouchSound( string sample )
        {
            if( !_Controller.IsInitialized )
                return;

            QSoundFX sfx = FindName( sample );
            Cache.Check( sfx.cache );
        }

        // S_ClearBuffer (void)
        public static void ClearBuffer()
        {
            if( !_Controller.IsInitialized || _shm == null || _shm.buffer == null )
                return;

            _Controller.ClearBuffer();
        }

        // S_StaticSound (QSoundFX *sfx, vec3_t origin, float vol, float attenuation)
        public static void StaticSound( QSoundFX sfx, ref Vector3 origin, float vol, float attenuation )
        {
            if( sfx == null )
                return;

            if( _TotalChannels == MAX_CHANNELS )
            {
                Con.Print( "total_channels == MAX_CHANNELS\n" );
                return;
            }

            QSoundChannel ss = _Channels[_TotalChannels];
            _TotalChannels++;

            QSoundFXCache sc = LoadSound( sfx );
            if( sc == null )
                return;

            if( sc.loopstart == -1 )
            {
                Con.Print( "Sound {0} not looped\n", sfx.name );
                return;
            }

            ss.sfx        = sfx;
            ss.origin     = origin;
            ss.master_vol = (int) vol;
            ss.dist_mult  = ( attenuation / 64 ) / _SoundNominalClipDist;
            ss.end        = _PaintedTime + sc.length;

            Spatialize( ss );
        }

        // S_StartSound (int entnum, int entchannel, QSoundFX *sfx, vec3_t origin, float fvol,  float attenuation)
        public static void StartSound( int entnum, int entchannel, QSoundFX sfx, ref Vector3 origin, float fvol, float attenuation )
        {
            if( !_SoundStarted || sfx == null )
                return;

            if( _NoSound.Value != 0 )
                return;

            int vol = (int) ( fvol * 255 );

            // pick a channel to play on
            QSoundChannel target_chan = PickChannel( entnum, entchannel );
            if( target_chan == null )
                return;

            // spatialize
            //memset (target_chan, 0, sizeof(*target_chan));
            target_chan.origin     = origin;
            target_chan.dist_mult  = attenuation / _SoundNominalClipDist;
            target_chan.master_vol = vol;
            target_chan.entnum     = entnum;
            target_chan.entchannel = entchannel;
            Spatialize( target_chan );

            if( target_chan.leftvol == 0 && target_chan.rightvol == 0 )
                return; // not audible at all

            // new channel
            QSoundFXCache sc = LoadSound( sfx );
            if( sc == null )
            {
                target_chan.sfx = null;
                return; // couldn't load the sound's data
            }

            target_chan.sfx = sfx;
            target_chan.pos = 0;
            target_chan.end = _PaintedTime + sc.length;

            // if an identical sound has also been started this frame, offset the pos
            // a bit to keep it from just making the first one louder
            for( int i = QBSPAmbientFlag.NUM_AMBIENTS; i < QBSPAmbientFlag.NUM_AMBIENTS + MAX_DYNAMIC_CHANNELS; i++ )
            {
                QSoundChannel check = _Channels[i];
                if( check == target_chan )
                    continue;

                if( check.sfx == sfx && check.pos == 0 )
                {
                    int skip = sys.Random( (int) ( 0.1 * _shm.speed ) ); // rand() % (int)(0.1 * shm->speed);
                    if( skip >= target_chan.end )
                        skip = target_chan.end - 1;
                    target_chan.pos += skip;
                    target_chan.end -= skip;
                    break;
                }
            }
        }

        // S_StopSound (int entnum, int entchannel)
        public static void StopSound( int entnum, int entchannel )
        {
            for( int i = 0; i < MAX_DYNAMIC_CHANNELS; i++ )
            {
                if( _Channels[i].entnum     == entnum &&
                    _Channels[i].entchannel == entchannel )
                {
                    _Channels[i].end = 0;
                    _Channels[i].sfx = null;
                    return;
                }
            }
        }

        // QSoundFX *S_PrecacheSound (char *sample)
        public static QSoundFX PrecacheSound( string sample )
        {
            if( !_IsInitialized || _NoSound.Value != 0 )
                return null;

            QSoundFX sfx = FindName( sample );

            // cache it in
            if( _Precache.Value != 0 )
                LoadSound( sfx );

            return sfx;
        }

        // void S_ClearPrecache (void)
        public static void ClearPrecache()
        {
            // nothing to do
        }

        // void S_Update (vec3_t origin, vec3_t v_forward, vec3_t v_right, vec3_t v_up)
        //
        // Called once each time through the main loop
        public static void Update( ref Vector3 origin, ref Vector3 forward, ref Vector3 right, ref Vector3 up )
        {
            if( !_IsInitialized || ( _SoundBlocked > 0 ) )
                return;

            _ListenerOrigin  = origin;
            _ListenerForward = forward;
            _ListenerRight   = right;
            _ListenerUp      = up;

            // update general area ambient sound sources
            UpdateAmbientSounds();

            QSoundChannel combine = null;

            // update spatialization for static and dynamic sounds
            //QSoundChannel ch = channels + NUM_AMBIENTS;
            for( int i = QBSPAmbientFlag.NUM_AMBIENTS; i < _TotalChannels; i++ )
            {
                QSoundChannel ch = _Channels[i]; // channels + NUM_AMBIENTS;
                if( ch.sfx == null )
                    continue;

                Spatialize( ch ); // respatialize channel
                if( ch.leftvol == 0 && ch.rightvol == 0 )
                    continue;

                // try to combine static sounds with a previous channel of the same
                // sound effect so we don't mix five torches every frame
                if( i >= MAX_DYNAMIC_CHANNELS + QBSPAmbientFlag.NUM_AMBIENTS )
                {
                    // see if it can just use the last one
                    if( combine != null && combine.sfx == ch.sfx )
                    {
                        combine.leftvol  += ch.leftvol;
                        combine.rightvol += ch.rightvol;
                        ch.leftvol       =  ch.rightvol = 0;
                        continue;
                    }

                    // search for one
                    combine = _Channels[MAX_DYNAMIC_CHANNELS + QBSPAmbientFlag.NUM_AMBIENTS]; // channels + MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS;
                    int j;
                    for( j = MAX_DYNAMIC_CHANNELS + QBSPAmbientFlag.NUM_AMBIENTS; j < i; j++ )
                    {
                        combine = _Channels[j];
                        if( combine.sfx == ch.sfx )
                            break;
                    }

                    if( j == _TotalChannels )
                    {
                        combine = null;
                    }
                    else
                    {
                        if( combine != ch )
                        {
                            combine.leftvol  += ch.leftvol;
                            combine.rightvol += ch.rightvol;
                            ch.leftvol       =  ch.rightvol = 0;
                        }

                        continue;
                    }
                }
            }

            //
            // debugging output
            //
            if( _Show.Value != 0 )
            {
                int total = 0;
                for( int i = 0; i < _TotalChannels; i++ )
                {
                    QSoundChannel ch = _Channels[i];
                    if( ch.sfx != null && ( ch.leftvol > 0 || ch.rightvol > 0 ) )
                    {
                        total++;
                    }
                }

                Con.Print( "----({0})----\n", total );
            }

            // mix some sound
            Update();
        }

        // S_StopAllSounds (qboolean clear)
        public static void StopAllSounds( bool clear )
        {
            if( !_Controller.IsInitialized )
                return;

            _TotalChannels = MAX_DYNAMIC_CHANNELS + QBSPAmbientFlag.NUM_AMBIENTS; // no statics

            for( int i = 0; i < MAX_CHANNELS; i++ )
                if( _Channels[i].sfx != null )
                    _Channels[i].Clear();

            if( clear )
                ClearBuffer();
        }

        // void S_BeginPrecaching (void)
        public static void BeginPrecaching()
        {
        }

        // void S_EndPrecaching (void)
        public static void EndPrecaching()
        {
        }

        // void S_ExtraUpdate (void)
        public static void ExtraUpdate()
        {
            if( !_IsInitialized )
                return;
#if _WIN32
	        IN_Accumulate ();
#endif

            if( _NoExtraUpdate.Value != 0 )
                return; // don't pollute timings

            Update();
        }

        // void S_LocalSound (char *s)
        public static void LocalSound( string sound )
        {
            if( _NoSound.Value != 0 )
                return;

            if( !_Controller.IsInitialized )
                return;

            QSoundFX sfx = PrecacheSound( sound );
            if( sfx == null )
            {
                Con.Print( "S_LocalSound: can't cache {0}\n", sound );
                return;
            }

            StartSound( QClient.cl.viewentity, -1, sfx, ref QCommon.ZeroVector, 1, 1 );
        }

        // S_Startup
        public static void Startup()
        {
            if( _IsInitialized && !_Controller.IsInitialized )
            {
                _Controller.Init();
                _SoundStarted = _Controller.IsInitialized;
            }
        }

        /// <summary>
        /// S_BlockSound
        /// </summary>
        public static void BlockSound()
        {
            _SoundBlocked++;

            if( _SoundBlocked == 1 )
            {
                _Controller.ClearBuffer(); //waveOutReset (hWaveOut);
            }
        }

        /// <summary>
        /// S_UnblockSound
        /// </summary>
        public static void UnblockSound()
        {
            _SoundBlocked--;
        }

        // S_Play
        private static void Play()
        {
            for( int i = 1; i < QCommand.Argc; i++ )
            {
                string name = QCommand.Argv( i );
                int    k    = name.IndexOf( '.' );
                if( k == -1 )
                    name += ".wav";

                QSoundFX sfx = PrecacheSound( name );
                StartSound( _PlayHash++, 0, sfx, ref _ListenerOrigin, 1.0f, 1.0f );
            }
        }

        // S_PlayVol
        private static void PlayVol()
        {
            for( int i = 1; i < QCommand.Argc; i += 2 )
            {
                string name = QCommand.Argv( i );
                int    k    = name.IndexOf( '.' );
                if( k == -1 )
                    name += ".wav";

                QSoundFX sfx = PrecacheSound( name );
                float    vol = float.Parse( QCommand.Argv( i + 1 ) );
                StartSound( _PlayVolHash++, 0, sfx, ref _ListenerOrigin, vol, 1.0f );
            }
        }

        // S_SoundList
        private static void SoundList()
        {
            int total = 0;
            for( int i = 0; i < _NumSfx; i++ )
            {
                QSoundFX      sfx = _KnownSfx[i];
                QSoundFXCache sc  = (QSoundFXCache) Cache.Check( sfx.cache );
                if( sc == null )
                    continue;

                int size = sc.length * sc.width * ( sc.stereo + 1 );
                total += size;
                if( sc.loopstart >= 0 )
                    Con.Print( "L" );
                else
                    Con.Print( " " );
                Con.Print( "({0:d2}b) {1:g6} : {2}\n", sc.width * 8, size, sfx.name );
            }

            Con.Print( "Total resident: {0}\n", total );
        }

        // S_SoundInfo_f
        private static void SoundInfo_f()
        {
            if( !_Controller.IsInitialized || _shm == null )
            {
                Con.Print( "sound system not started\n" );
                return;
            }

            Con.Print( "{0:d5} stereo\n",           _shm.channels - 1 );
            Con.Print( "{0:d5} samples\n",          _shm.samples );
            Con.Print( "{0:d5} samplepos\n",        _shm.samplepos );
            Con.Print( "{0:d5} samplebits\n",       _shm.samplebits );
            Con.Print( "{0:d5} submission_chunk\n", _shm.submission_chunk );
            Con.Print( "{0:d5} speed\n",            _shm.speed );
            //Con.Print("0x%x dma buffer\n", _shm.buffer);
            Con.Print( "{0:d5} total_channels\n", _TotalChannels );
        }

        // S_StopAllSoundsC
        private static void StopAllSoundsCmd()
        {
            StopAllSounds( true );
        }

        // S_FindName
        private static QSoundFX FindName( string name )
        {
            if( String.IsNullOrEmpty( name ) )
                sys.Error( "S_FindName: NULL or empty\n" );

            if( name.Length >= QDef.MAX_QPATH )
                sys.Error( "Sound name too long: {0}", name );

            // see if already loaded
            for( int i = 0; i < _NumSfx; i++ )
            {
                if( _KnownSfx[i].name == name ) // !Q_strcmp(known_sfx[i].name, name))
                    return _KnownSfx[i];
            }

            if( _NumSfx == MAX_SFX )
                sys.Error( "S_FindName: out of QSoundFX" );

            QSoundFX sfx = _KnownSfx[_NumSfx];
            sfx.name = name;

            _NumSfx++;
            return sfx;
        }

        // SND_Spatialize
        private static void Spatialize( QSoundChannel ch )
        {
            // anything coming from the view entity will allways be full volume
            if( ch.entnum == QClient.cl.viewentity )
            {
                ch.leftvol  = ch.master_vol;
                ch.rightvol = ch.master_vol;
                return;
            }

            // calculate stereo seperation and distance attenuation
            QSoundFX snd        = ch.sfx;
            Vector3  source_vec = ch.origin - _ListenerOrigin;

            float dist = mathlib.Normalize( ref source_vec ) * ch.dist_mult;
            float dot  = Vector3.Dot( _ListenerRight, source_vec );

            float rscale, lscale;
            if( _shm.channels == 1 )
            {
                rscale = 1.0f;
                lscale = 1.0f;
            }
            else
            {
                rscale = 1.0f + dot;
                lscale = 1.0f - dot;
            }

            // add in distance effect
            float scale = ( 1.0f - dist )       * rscale;
            ch.rightvol = (int) ( ch.master_vol * scale );
            if( ch.rightvol < 0 )
                ch.rightvol = 0;

            scale      = ( 1.0f - dist ) * lscale;
            ch.leftvol = (int) ( ch.master_vol * scale );
            if( ch.leftvol < 0 )
                ch.leftvol = 0;
        }

        // S_LoadSound
        private static QSoundFXCache LoadSound( QSoundFX s )
        {
            // see if still in memory
            QSoundFXCache sc = (QSoundFXCache) Cache.Check( s.cache );
            if( sc != null )
                return sc;

            // load it in
            string namebuffer = "sound/" + s.name;

            byte[] data = QCommon.LoadFile( namebuffer );
            if( data == null )
            {
                Con.Print( "Couldn't load {0}\n", namebuffer );
                return null;
            }

            QSoundWAVInfo info = GetWavInfo( s.name, data );
            if( info.channels != 1 )
            {
                Con.Print( "{0} is a stereo sample\n", s.name );
                return null;
            }

            float stepscale = info.rate / (float) _shm.speed;
            int   len       = (int) ( info.samples / stepscale );

            len *= info.width * info.channels;

            s.cache = Cache.Alloc( len, s.name );
            if( s.cache == null )
                return null;

            sc           = new QSoundFXCache();
            sc.length    = info.samples;
            sc.loopstart = info.loopstart;
            sc.speed     = info.rate;
            sc.width     = info.width;
            sc.stereo    = info.channels;
            s.cache.data = sc;

            ResampleSfx( s, sc.speed, sc.width, new QByteArraySegment( data, info.dataofs ) );

            return sc;
        }

        // SND_PickChannel
        private static QSoundChannel PickChannel( int entnum, int entchannel )
        {
            // Check for replacement sound, or find the best one to replace
            int first_to_die = -1;
            int life_left    = 0x7fffffff;
            for( int ch_idx = QBSPAmbientFlag.NUM_AMBIENTS; ch_idx < QBSPAmbientFlag.NUM_AMBIENTS + MAX_DYNAMIC_CHANNELS; ch_idx++ )
            {
                if( entchannel                  != 0 // channel 0 never overrides
                    && _Channels[ch_idx].entnum == entnum
                    && ( _Channels[ch_idx].entchannel == entchannel || entchannel == -1 ) )
                {
                    // allways override sound from same entity
                    first_to_die = ch_idx;
                    break;
                }

                // don't let monster sounds override player sounds
                if( _Channels[ch_idx].entnum == QClient.cl.viewentity && entnum != QClient.cl.viewentity && _Channels[ch_idx].sfx != null )
                    continue;

                if( _Channels[ch_idx].end - _PaintedTime < life_left )
                {
                    life_left    = _Channels[ch_idx].end - _PaintedTime;
                    first_to_die = ch_idx;
                }
            }

            if( first_to_die == -1 )
                return null;

            //if( _Channels[first_to_die].sfx != null )
            _Channels[first_to_die].sfx = null;

            return _Channels[first_to_die];
        }

        // S_UpdateAmbientSounds
        private static void UpdateAmbientSounds()
        {
            if( !_Ambient )
                return;

            // calc ambient sound levels
            if( QClient.cl.worldmodel == null )
                return;

            mleaf_t l = Mod.PointInLeaf( ref _ListenerOrigin, QClient.cl.worldmodel );
            if( l == null || _AmbientLevel.Value == 0 )
            {
                for( int i = 0; i < QBSPAmbientFlag.NUM_AMBIENTS; i++ )
                    _Channels[i].sfx = null;
                return;
            }

            for( int i = 0; i < QBSPAmbientFlag.NUM_AMBIENTS; i++ )
            {
                QSoundChannel chan = _Channels[i];
                chan.sfx = _AmbientSfx[i];

                float vol = _AmbientLevel.Value * l.ambient_sound_level[i];
                if( vol < 8 )
                    vol = 0;

                // don't adjust volume too fast
                if( chan.master_vol < vol )
                {
                    chan.master_vol += (int) ( host.FrameTime * _AmbientFade.Value );
                    if( chan.master_vol > vol )
                        chan.master_vol = (int) vol;
                }
                else if( chan.master_vol > vol )
                {
                    chan.master_vol -= (int) ( host.FrameTime * _AmbientFade.Value );
                    if( chan.master_vol < vol )
                        chan.master_vol = (int) vol;
                }

                chan.leftvol = chan.rightvol = chan.master_vol;
            }
        }

        // S_Update_
        private static void Update()
        {
            if( !_SoundStarted || ( _SoundBlocked > 0 ) )
                return;

            // Updates DMA time
            GetSoundTime();

            // check to make sure that we haven't overshot
            if( _PaintedTime < _SoundTime )
                _PaintedTime = _SoundTime;

            // mix ahead of current position
            int endtime = (int) ( _SoundTime              + _MixAhead.Value * _shm.speed );
            int samps   = _shm.samples >> ( _shm.channels - 1 );
            if( endtime - _SoundTime > samps )
                endtime = _SoundTime + samps;

            PaintChannels( endtime );
        }

        // GetSoundtime
        private static void GetSoundTime()
        {
            int fullsamples = _shm.samples / _shm.channels;
            int samplepos   = _Controller.GetPosition();
            if( samplepos < _OldSamplePos )
            {
                _Buffers++; // buffer wrapped

                if( _PaintedTime > 0x40000000 )
                {
                    // time to chop things off to avoid 32 bit limits
                    _Buffers     = 0;
                    _PaintedTime = fullsamples;
                    StopAllSounds( true );
                }
            }

            _OldSamplePos = samplepos;
            _SoundTime    = _Buffers * fullsamples + samplepos / _shm.channels;
        }

        static QSound()
        {
            for( int i = 0; i < _KnownSfx.Length; i++ )
                _KnownSfx[i] = new QSoundFX();
        }
    }
}
