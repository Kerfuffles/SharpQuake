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

using System;
using System.Globalization;
using System.IO;
using System.Text;

// host_cmd.c

namespace SharpQuake
{
    partial class host
    {
        /// <summary>
        /// Host_Quit_f
        /// </summary>
        public static void Quit_f()
        {
            if( Key.Destination != keydest_t.key_console && QClient.cls.state != ServerType.DEDICATED )
            {
                MenuBase.QuitMenu.Show();
                return;
            }
            QClient.Disconnect();
            host.ShutdownServer( false );
            sys.Quit();
        }

        // Host_InitCommands
        private static void InitCommands()
        {
            cmd.Add( "status", Status_f );
            cmd.Add( "quit", Quit_f );
            cmd.Add( "god", God_f );
            cmd.Add( "notarget", Notarget_f );
            cmd.Add( "fly", Fly_f );
            cmd.Add( "map", Map_f );
            cmd.Add( "restart", Restart_f );
            cmd.Add( "changelevel", Changelevel_f );
            cmd.Add( "connect", Connect_f );
            cmd.Add( "reconnect", Reconnect_f );
            cmd.Add( "name", Name_f );
            cmd.Add( "noclip", Noclip_f );
            cmd.Add( "version", Version_f );
            cmd.Add( "say", Say_f );
            cmd.Add( "say_team", Say_Team_f );
            cmd.Add( "tell", Tell_f );
            cmd.Add( "color", Color_f );
            cmd.Add( "kill", Kill_f );
            cmd.Add( "pause", Pause_f );
            cmd.Add( "spawn", Spawn_f );
            cmd.Add( "begin", Begin_f );
            cmd.Add( "prespawn", PreSpawn_f );
            cmd.Add( "kick", Kick_f );
            cmd.Add( "ping", Ping_f );
            cmd.Add( "load", Loadgame_f );
            cmd.Add( "save", Savegame_f );
            cmd.Add( "give", Give_f );

            cmd.Add( "startdemos", Startdemos_f );
            cmd.Add( "demos", Demos_f );
            cmd.Add( "stopdemo", Stopdemo_f );

            cmd.Add( "viewmodel", Viewmodel_f );
            cmd.Add( "viewframe", Viewframe_f );
            cmd.Add( "viewnext", Viewnext_f );
            cmd.Add( "viewprev", Viewprev_f );

            cmd.Add( "mcache", Mod.Print );
        }

        /// <summary>
        /// Host_Status_f
        /// </summary>
        private static void Status_f()
        {
            bool flag = true;
            if( cmd.Source == cmd_source_t.src_command )
            {
                if( !server.sv.active )
                {
                    cmd.ForwardToServer();
                    return;
                }
            }
            else
                flag = false;

            StringBuilder sb = new StringBuilder( 256 );
            sb.Append( $"host:    {cvar.GetString( "hostname" )}\n" );
            sb.Append( $"version: {QDef.VERSION:F2}\n" );
            if( net.TcpIpAvailable )
            {
                sb.Append( "tcp/ip:  " );
                sb.Append( net.MyTcpIpAddress );
                sb.Append( '\n' );
            }

            sb.Append( "map:     " );
            sb.Append( server.sv.name );
            sb.Append( '\n' );
            sb.Append( $"players: {net.ActiveConnections} active ({server.svs.maxclients} max)\n\n" );
            for( int j = 0; j < server.svs.maxclients; j++ )
            {
                client_t client = server.svs.clients[j];
                if( !client.active )
                    continue;

                int seconds = (int)( net.Time - client.netconnection.connecttime );
                int hours, minutes = seconds / 60;
                if( minutes > 0 )
                {
                    seconds -= ( minutes * 60 );
                    hours = minutes / 60;
                    if( hours > 0 )
                        minutes -= ( hours * 60 );
                }
                else
                    hours = 0;
                sb.Append( string.Format( "#{0,-2} {1,-16}  {2}  {2}:{4,2}:{5,2}",
                    j + 1, client.name, (int)client.edict.v.frags, hours, minutes, seconds ) );
                sb.Append( "   " );
                sb.Append( client.netconnection.address );
                sb.Append( '\n' );
            }

            if( flag )
                Con.Print( sb.ToString() );
            else
                server.ClientPrint( sb.ToString() );
        }

        /// <summary>
        /// Host_God_f
        /// Sets QClient to godmode
        /// </summary>
        private static void God_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                cmd.ForwardToServer();
                return;
            }

            if( progs.GlobalStruct.deathmatch != 0 && !host.HostClient.privileged )
                return;

            server.Player.v.flags = (int)server.Player.v.flags ^ EdictFlags.FL_GODMODE;
            if( ( (int)server.Player.v.flags & EdictFlags.FL_GODMODE ) == 0 )
                server.ClientPrint( "godmode OFF\n" );
            else
                server.ClientPrint( "godmode ON\n" );
        }

        /// <summary>
        /// Host_Notarget_f
        /// </summary>
        private static void Notarget_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                cmd.ForwardToServer();
                return;
            }

            if( progs.GlobalStruct.deathmatch != 0 && !host.HostClient.privileged )
                return;

            server.Player.v.flags = (int)server.Player.v.flags ^ EdictFlags.FL_NOTARGET;
            if( ( (int)server.Player.v.flags & EdictFlags.FL_NOTARGET ) == 0 )
                server.ClientPrint( "notarget OFF\n" );
            else
                server.ClientPrint( "notarget ON\n" );
        }

        /// <summary>
        /// Host_Noclip_f
        /// </summary>
        private static void Noclip_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                cmd.ForwardToServer();
                return;
            }

            if( progs.GlobalStruct.deathmatch > 0 && !host.HostClient.privileged )
                return;

            if( server.Player.v.movetype != Movetypes.MOVETYPE_NOCLIP )
            {
                host.NoClipAngleHack = true;
                server.Player.v.movetype = Movetypes.MOVETYPE_NOCLIP;
                server.ClientPrint( "noclip ON\n" );
            }
            else
            {
                host.NoClipAngleHack = false;
                server.Player.v.movetype = Movetypes.MOVETYPE_WALK;
                server.ClientPrint( "noclip OFF\n" );
            }
        }

        /// <summary>
        /// Host_Fly_f
        /// Sets QClient to flymode
        /// </summary>
        private static void Fly_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                cmd.ForwardToServer();
                return;
            }

            if( progs.GlobalStruct.deathmatch > 0 && !host.HostClient.privileged )
                return;

            if( server.Player.v.movetype != Movetypes.MOVETYPE_FLY )
            {
                server.Player.v.movetype = Movetypes.MOVETYPE_FLY;
                server.ClientPrint( "flymode ON\n" );
            }
            else
            {
                server.Player.v.movetype = Movetypes.MOVETYPE_WALK;
                server.ClientPrint( "flymode OFF\n" );
            }
        }

        /// <summary>
        /// Host_Ping_f
        /// </summary>
        private static void Ping_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                cmd.ForwardToServer();
                return;
            }

            server.ClientPrint( "Client ping times:\n" );
            for( int i = 0; i < server.svs.maxclients; i++ )
            {
                client_t client = server.svs.clients[i];
                if( !client.active )
                    continue;
                float total = 0;
                for( int j = 0; j < server.NUM_PING_TIMES; j++ )
                    total += client.ping_times[j];
                total /= server.NUM_PING_TIMES;
                server.ClientPrint( "{0,4} {1}\n", (int)( total * 1000 ), client.name );
            }
        }

        // Host_Map_f
        //
        // handle a
        // map <servername>
        // command from the console.  Active clients are kicked off.
        private static void Map_f()
        {
            if( cmd.Source != cmd_source_t.src_command )
                return;

            QClient.cls.demonum = -1;		// stop demo loop in case this fails

            QClient.Disconnect();
            ShutdownServer( false );

            Key.Destination = keydest_t.key_game;			// remove console or menu
            Scr.BeginLoadingPlaque();

            QClient.cls.mapstring = cmd.JoinArgv() + "\n";

            server.svs.serverflags = 0;			// haven't completed an episode yet
            string name = cmd.Argv( 1 );
            server.SpawnServer( name );

            if( !server.IsActive )
                return;

            if( QClient.cls.state != ServerType.DEDICATED )
            {
                QClient.cls.spawnparms = cmd.JoinArgv();
                cmd.ExecuteString( "connect local", cmd_source_t.src_command );
            }
        }

        /// <summary>
        /// Host_Changelevel_f
        /// Goes to a new map, taking all clients along
        /// </summary>
        private static void Changelevel_f()
        {
            if( cmd.Argc != 2 )
            {
                Con.Print( "changelevel <levelname> : continue game on a new level\n" );
                return;
            }
            if( !server.sv.active || QClient.cls.demoplayback )
            {
                Con.Print( "Only the server may changelevel\n" );
                return;
            }
            server.SaveSpawnparms();
            string level = cmd.Argv( 1 );
            server.SpawnServer( level );
        }

        // Host_Restart_f
        //
        // Restarts the current server for a dead player
        private static void Restart_f()
        {
            if( QClient.cls.demoplayback || !server.IsActive )
                return;

            if( cmd.Source != cmd_source_t.src_command )
                return;

            string mapname = server.sv.name; // must copy out, because it gets cleared
                                             // in sv_spawnserver
            server.SpawnServer( mapname );
        }

        /// <summary>
        /// Host_Reconnect_f
        /// This command causes the QClient to wait for the signon messages again.
        /// This is sent just before a server changes levels
        /// </summary>
        private static void Reconnect_f()
        {
            Scr.BeginLoadingPlaque();
            QClient.cls.signon = 0;		// need new connection messages
        }

        /// <summary>
        /// Host_Connect_f
        /// User command to connect to server
        /// </summary>
        private static void Connect_f()
        {
            QClient.cls.demonum = -1;		// stop demo loop in case this fails
            if( QClient.cls.demoplayback )
            {
                QClient.StopPlayback();
                QClient.Disconnect();
            }
            string name = cmd.Argv( 1 );
            QClient.EstablishConnection( name );
            Reconnect_f();
        }

        /// <summary>
        /// Host_SavegameComment
        /// Writes a SAVEGAME_COMMENT_LENGTH character comment describing the current
        /// </summary>
        private static string SavegameComment()
        {
            string result = $"{QClient.cl.levelname} kills:{QClient.cl.stats[QStats.STAT_MONSTERS],3}/{QClient.cl.stats[QStats.STAT_TOTALMONSTERS],3}";

            // convert space to _ to make stdio happy
            result = result.Replace( ' ', '_' );

            if( result.Length < QDef.SAVEGAME_COMMENT_LENGTH - 1 )
                result = result.PadRight( QDef.SAVEGAME_COMMENT_LENGTH - 1, '_' );

            if( result.Length > QDef.SAVEGAME_COMMENT_LENGTH - 1 )
                result = result.Remove( QDef.SAVEGAME_COMMENT_LENGTH - 2 );

            return result + '\0';
        }

        /// <summary>
        /// Host_Savegame_f
        /// </summary>
        private static void Savegame_f()
        {
            if( cmd.Source != cmd_source_t.src_command )
                return;

            if( !server.sv.active )
            {
                Con.Print( "Not playing a local game.\n" );
                return;
            }

            if( QClient.cl.intermission != 0 )
            {
                Con.Print( "Can't save in intermission.\n" );
                return;
            }

            if( server.svs.maxclients != 1 )
            {
                Con.Print( "Can't save multiplayer games.\n" );
                return;
            }

            if( cmd.Argc != 2 )
            {
                Con.Print( "save <savename> : save a game\n" );
                return;
            }

            if( cmd.Argv( 1 ).Contains( ".." ) )
            {
                Con.Print( "Relative pathnames are not allowed.\n" );
                return;
            }

            for( int i = 0; i < server.svs.maxclients; i++ )
            {
                if( server.svs.clients[i].active && ( server.svs.clients[i].edict.v.health <= 0 ) )
                {
                    Con.Print( "Can't savegame with a dead player\n" );
                    return;
                }
            }

            string name = Path.ChangeExtension( Path.Combine( common.GameDir, cmd.Argv( 1 ) ), ".sav" );

            Con.Print( "Saving game to {0}...\n", name );
            FileStream fs = sys.FileOpenWrite( name, true );
            if( fs == null )
            {
                Con.Print( "ERROR: couldn't open.\n" );
                return;
            }
            using( StreamWriter writer = new StreamWriter( fs, Encoding.ASCII ) )
            {
                writer.WriteLine( SAVEGAME_VERSION );
                writer.WriteLine( SavegameComment() );

                for( int i = 0; i < server.NUM_SPAWN_PARMS; i++ )
                    writer.WriteLine( server.svs.clients[0].spawn_parms[i].ToString( "F6",
                        CultureInfo.InvariantCulture.NumberFormat ) );

                writer.WriteLine( host.CurrentSkill );
                writer.WriteLine( server.sv.name );
                writer.WriteLine( server.sv.time.ToString( "F6",
                    CultureInfo.InvariantCulture.NumberFormat ) );

                // write the light styles

                for( int i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
                {
                    if( !string.IsNullOrEmpty( server.sv.lightstyles[i] ) )
                        writer.WriteLine( server.sv.lightstyles[i] );
                    else
                        writer.WriteLine( "m" );
                }

                progs.WriteGlobals( writer );
                for( int i = 0; i < server.sv.num_edicts; i++ )
                {
                    progs.WriteEdict( writer, server.EdictNum( i ) );
                    writer.Flush();
                }
            }
            Con.Print( "done.\n" );
        }

        /// <summary>
        /// Host_Loadgame_f
        /// </summary>
        private static void Loadgame_f()
        {
            if( cmd.Source != cmd_source_t.src_command )
                return;

            if( cmd.Argc != 2 )
            {
                Con.Print( "load <savename> : load a game\n" );
                return;
            }

            QClient.cls.demonum = -1;		// stop demo loop in case this fails

            string name = Path.ChangeExtension( Path.Combine( common.GameDir, cmd.Argv( 1 ) ), ".sav" );

            // we can't call SCR_BeginLoadingPlaque, because too much stack space has
            // been used.  The menu calls it before stuffing loadgame command
            //	SCR_BeginLoadingPlaque ();

            Con.Print( "Loading game from {0}...\n", name );
            FileStream fs = sys.FileOpenRead( name );
            if( fs == null )
            {
                Con.Print( "ERROR: couldn't open.\n" );
                return;
            }

            using( StreamReader reader = new StreamReader( fs, Encoding.ASCII ) )
            {
                string line = reader.ReadLine();
                int version = common.atoi( line );
                if( version != SAVEGAME_VERSION )
                {
                    Con.Print( "Savegame is version {0}, not {1}\n", version, SAVEGAME_VERSION );
                    return;
                }
                line = reader.ReadLine();

                float[] spawn_parms = new float[server.NUM_SPAWN_PARMS];
                for( int i = 0; i < spawn_parms.Length; i++ )
                {
                    line = reader.ReadLine();
                    spawn_parms[i] = common.atof( line );
                }
                // this silliness is so we can load 1.06 save files, which have float skill values
                line = reader.ReadLine();
                float tfloat = common.atof( line );
                host.CurrentSkill = (int)( tfloat + 0.1 );
                cvar.Set( "skill", (float)host.CurrentSkill );

                string mapname = reader.ReadLine();
                line = reader.ReadLine();
                float time = common.atof( line );

                QClient.Disconnect_f();
                server.SpawnServer( mapname );

                if( !server.sv.active )
                {
                    Con.Print( "Couldn't load map\n" );
                    return;
                }
                server.sv.paused = true;		// pause until all clients connect
                server.sv.loadgame = true;

                // load the light styles

                for( int i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
                {
                    line = reader.ReadLine();
                    server.sv.lightstyles[i] = line;
                }

                // load the edicts out of the savegame file
                int entnum = -1;		// -1 is the globals
                StringBuilder sb = new StringBuilder( 32768 );
                while( !reader.EndOfStream )
                {
                    line = reader.ReadLine();
                    if( line == null )
                        sys.Error( "EOF without closing brace" );

                    sb.AppendLine( line );
                    int idx = line.IndexOf( '}' );
                    if( idx != -1 )
                    {
                        int length = 1 + sb.Length - ( line.Length - idx );
                        string data = common.Parse( sb.ToString( 0, length ) );
                        if( string.IsNullOrEmpty( common.Token ) )
                            break; // end of file
                        if( common.Token != "{" )
                            sys.Error( "First token isn't a brace" );

                        if( entnum == -1 )
                        {
                            // parse the global vars
                            progs.ParseGlobals( data );
                        }
                        else
                        {
                            // parse an edict
                            edict_t ent = server.EdictNum( entnum );
                            ent.Clear();
                            progs.ParseEdict( data, ent );

                            // link it into the bsp tree
                            if( !ent.free )
                                server.LinkEdict( ent, false );
                        }

                        entnum++;
                        sb.Remove( 0, length );
                    }
                }

                server.sv.num_edicts = entnum;
                server.sv.time = time;

                for( int i = 0; i < server.NUM_SPAWN_PARMS; i++ )
                    server.svs.clients[0].spawn_parms[i] = spawn_parms[i];
            }

            if( QClient.cls.state != ServerType.DEDICATED )
            {
                QClient.EstablishConnection( "local" );
                Reconnect_f();
            }
        }

        // Host_Name_f
        private static void Name_f()
        {
            if( cmd.Argc == 1 )
            {
                Con.Print( "\"name\" is \"{0}\"\n", QClient.Name );
                return;
            }

            string newName;
            if( cmd.Argc == 2 )
                newName = cmd.Argv( 1 );
            else
                newName = cmd.Args;

            if( newName.Length > 16 )
                newName = newName.Remove( 15 );

            if( cmd.Source == cmd_source_t.src_command )
            {
                if( QClient.Name == newName )
                    return;
                cvar.Set( "_cl_name", newName );
                if( QClient.cls.state == ServerType.CONNECTED )
                    cmd.ForwardToServer();
                return;
            }

            if( !string.IsNullOrEmpty( host.HostClient.name ) && host.HostClient.name != "unconnected" )
                if( host.HostClient.name != newName )
                    Con.Print( "{0} renamed to {1}\n", host.HostClient.name, newName );

            host.HostClient.name = newName;
            host.HostClient.edict.v.netname = progs.NewString( newName );

            // send notification to all clients
            MsgWriter msg = server.sv.reliable_datagram;
            msg.WriteByte( protocol.svc_updatename );
            msg.WriteByte( host.ClientNum );
            msg.WriteString( newName );
        }

        // Host_Version_f
        private static void Version_f()
        {
            Con.Print( "Version {0}\n", QDef.VERSION );
            Con.Print( "Exe hash code: {0}\n", System.Reflection.Assembly.GetExecutingAssembly().GetHashCode() );
        }

        /// <summary>
        /// Host_Say
        /// </summary>
        private static void Say( bool teamonly )
        {
            bool fromServer = false;
            if( cmd.Source == cmd_source_t.src_command )
            {
                if( QClient.cls.state == ServerType.DEDICATED )
                {
                    fromServer = true;
                    teamonly = false;
                }
                else
                {
                    cmd.ForwardToServer();
                    return;
                }
            }

            if( cmd.Argc < 2 )
                return;

            client_t save = host.HostClient;

            string p = cmd.Args;
            // remove quotes if present
            if( p.StartsWith( "\"" ) )
            {
                p = p.Substring( 1, p.Length - 2 );
            }

            // turn on color set 1
            string text;
            if( !fromServer )
                text = (char)1 + save.name + ": ";
            else
                text = (char)1 + "<" + net.HostName + "> ";

            text += p + "\n";

            for( int j = 0; j < server.svs.maxclients; j++ )
            {
                client_t client = server.svs.clients[j];
                if( client == null || !client.active || !client.spawned )
                    continue;
                if( host.TeamPlay != 0 && teamonly && client.edict.v.team != save.edict.v.team )
                    continue;
                host.HostClient = client;
                server.ClientPrint( text );
            }
            host.HostClient = save;
        }

        // Host_Say_f
        private static void Say_f()
        {
            Say( false );
        }

        // Host_Say_Team_f
        private static void Say_Team_f()
        {
            Say( true );
        }

        // Host_Tell_f
        private static void Tell_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                cmd.ForwardToServer();
                return;
            }

            if( cmd.Argc < 3 )
                return;

            string text = host.HostClient.name + ": ";
            string p = cmd.Args;

            // remove quotes if present
            if( p.StartsWith( "\"" ) )
            {
                p = p.Substring( 1, p.Length - 2 );
            }

            text += p + "\n";

            client_t save = host.HostClient;
            for( int j = 0; j < server.svs.maxclients; j++ )
            {
                client_t client = server.svs.clients[j];
                if( !client.active || !client.spawned )
                    continue;
                if( client.name == cmd.Argv( 1 ) )
                    continue;
                host.HostClient = client;
                server.ClientPrint( text );
                break;
            }
            host.HostClient = save;
        }

        // Host_Color_f
        private static void Color_f()
        {
            if( cmd.Argc == 1 )
            {
                Con.Print( "\"color\" is \"{0} {1}\"\n", ( (int)QClient.Color ) >> 4, ( (int)QClient.Color ) & 0x0f );
                Con.Print( "color <0-13> [0-13]\n" );
                return;
            }

            int top, bottom;
            if( cmd.Argc == 2 )
                top = bottom = common.atoi( cmd.Argv( 1 ) );
            else
            {
                top = common.atoi( cmd.Argv( 1 ) );
                bottom = common.atoi( cmd.Argv( 2 ) );
            }

            top &= 15;
            if( top > 13 )
                top = 13;
            bottom &= 15;
            if( bottom > 13 )
                bottom = 13;

            int playercolor = top * 16 + bottom;

            if( cmd.Source == cmd_source_t.src_command )
            {
                cvar.Set( "_cl_color", playercolor );
                if( QClient.cls.state == ServerType.CONNECTED )
                    cmd.ForwardToServer();
                return;
            }

            host.HostClient.colors = playercolor;
            host.HostClient.edict.v.team = bottom + 1;

            // send notification to all clients
            MsgWriter msg = server.sv.reliable_datagram;
            msg.WriteByte( protocol.svc_updatecolors );
            msg.WriteByte( host.ClientNum );
            msg.WriteByte( host.HostClient.colors );
        }

        /// <summary>
        /// Host_Kill_f
        /// </summary>
        private static void Kill_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                cmd.ForwardToServer();
                return;
            }

            if( server.Player.v.health <= 0 )
            {
                server.ClientPrint( "Can't suicide -- allready dead!\n" );
                return;
            }

            progs.GlobalStruct.time = (float)server.sv.time;
            progs.GlobalStruct.self = server.EdictToProg( server.Player );
            progs.Execute( progs.GlobalStruct.ClientKill );
        }

        /// <summary>
        /// Host_Pause_f
        /// </summary>
        private static void Pause_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                cmd.ForwardToServer();
                return;
            }
            if( _Pausable.Value == 0 )
                server.ClientPrint( "Pause not allowed.\n" );
            else
            {
                server.sv.paused = !server.sv.paused;

                if( server.sv.paused )
                {
                    server.BroadcastPrint( "{0} paused the game\n", progs.GetString( server.Player.v.netname ) );
                }
                else
                {
                    server.BroadcastPrint( "{0} unpaused the game\n", progs.GetString( server.Player.v.netname ) );
                }

                // send notification to all clients
                server.sv.reliable_datagram.WriteByte( protocol.svc_setpause );
                server.sv.reliable_datagram.WriteByte( server.sv.paused ? 1 : 0 );
            }
        }

        /// <summary>
        /// Host_PreSpawn_f
        /// </summary>
        private static void PreSpawn_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                Con.Print( "prespawn is not valid from the console\n" );
                return;
            }

            if( host.HostClient.spawned )
            {
                Con.Print( "prespawn not valid -- allready spawned\n" );
                return;
            }

            MsgWriter msg = host.HostClient.message;
            msg.Write( server.sv.signon.Data, 0, server.sv.signon.Length );
            msg.WriteByte( protocol.svc_signonnum );
            msg.WriteByte( 2 );
            host.HostClient.sendsignon = true;
        }

        /// <summary>
        /// Host_Spawn_f
        /// </summary>
        private static void Spawn_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                Con.Print( "spawn is not valid from the console\n" );
                return;
            }

            if( host.HostClient.spawned )
            {
                Con.Print( "Spawn not valid -- allready spawned\n" );
                return;
            }

            edict_t ent;

            // run the entrance script
            if( server.sv.loadgame )
            {
                // loaded games are fully inited allready
                // if this is the last QClient to be connected, unpause
                server.sv.paused = false;
            }
            else
            {
                // set up the edict
                ent = host.HostClient.edict;

                ent.Clear(); //memset(&ent.v, 0, progs.entityfields * 4);
                ent.v.colormap = server.NumForEdict( ent );
                ent.v.team = ( host.HostClient.colors & 15 ) + 1;
                ent.v.netname = progs.NewString( host.HostClient.name );

                // copy spawn parms out of the client_t
                progs.GlobalStruct.SetParams( host.HostClient.spawn_parms );

                // call the spawn function

                progs.GlobalStruct.time = (float)server.sv.time;
                progs.GlobalStruct.self = server.EdictToProg( server.Player );
                progs.Execute( progs.GlobalStruct.ClientConnect );

                if( ( sys.GetFloatTime() - host.HostClient.netconnection.connecttime ) <= server.sv.time )
                    Con.DPrint( "{0} entered the game\n", host.HostClient.name );

                progs.Execute( progs.GlobalStruct.PutClientInServer );
            }

            // send all current names, colors, and frag counts
            MsgWriter msg = host.HostClient.message;
            msg.Clear();

            // send time of update
            msg.WriteByte( protocol.svc_time );
            msg.WriteFloat( (float)server.sv.time );

            for( int i = 0; i < server.svs.maxclients; i++ )
            {
                client_t client = server.svs.clients[i];
                msg.WriteByte( protocol.svc_updatename );
                msg.WriteByte( i );
                msg.WriteString( client.name );
                msg.WriteByte( protocol.svc_updatefrags );
                msg.WriteByte( i );
                msg.WriteShort( client.old_frags );
                msg.WriteByte( protocol.svc_updatecolors );
                msg.WriteByte( i );
                msg.WriteByte( client.colors );
            }

            // send all current light styles
            for( int i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
            {
                msg.WriteByte( protocol.svc_lightstyle );
                msg.WriteByte( (char)i );
                msg.WriteString( server.sv.lightstyles[i] );
            }

            //
            // send some stats
            //
            msg.WriteByte( protocol.svc_updatestat );
            msg.WriteByte( QStats.STAT_TOTALSECRETS );
            msg.WriteLong( (int)progs.GlobalStruct.total_secrets );

            msg.WriteByte( protocol.svc_updatestat );
            msg.WriteByte( QStats.STAT_TOTALMONSTERS );
            msg.WriteLong( (int)progs.GlobalStruct.total_monsters );

            msg.WriteByte( protocol.svc_updatestat );
            msg.WriteByte( QStats.STAT_SECRETS );
            msg.WriteLong( (int)progs.GlobalStruct.found_secrets );

            msg.WriteByte( protocol.svc_updatestat );
            msg.WriteByte( QStats.STAT_MONSTERS );
            msg.WriteLong( (int)progs.GlobalStruct.killed_monsters );

            //
            // send a fixangle
            // Never send a roll angle, because savegames can catch the server
            // in a state where it is expecting the QClient to correct the angle
            // and it won't happen if the game was just loaded, so you wind up
            // with a permanent head tilt
            ent = server.EdictNum( 1 + host.ClientNum );
            msg.WriteByte( protocol.svc_setangle );
            msg.WriteAngle( ent.v.angles.x );
            msg.WriteAngle( ent.v.angles.y );
            msg.WriteAngle( 0 );

            server.WriteClientDataToMessage( server.Player, host.HostClient.message );

            msg.WriteByte( protocol.svc_signonnum );
            msg.WriteByte( 3 );
            host.HostClient.sendsignon = true;
        }

        // Host_Begin_f
        private static void Begin_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                Con.Print( "begin is not valid from the console\n" );
                return;
            }

            host.HostClient.spawned = true;
        }

        /// <summary>
        /// Host_Kick_f
        /// Kicks a user off of the server
        /// </summary>
        private static void Kick_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                if( !server.sv.active )
                {
                    cmd.ForwardToServer();
                    return;
                }
            }
            else if( progs.GlobalStruct.deathmatch != 0 && !host.HostClient.privileged )
                return;

            client_t save = host.HostClient;
            bool byNumber = false;
            int i;
            if( cmd.Argc > 2 && cmd.Argv( 1 ) == "#" )
            {
                i = (int)common.atof( cmd.Argv( 2 ) ) - 1;
                if( i < 0 || i >= server.svs.maxclients )
                    return;
                if( !server.svs.clients[i].active )
                    return;

                host.HostClient = server.svs.clients[i];
                byNumber = true;
            }
            else
            {
                for( i = 0; i < server.svs.maxclients; i++ )
                {
                    host.HostClient = server.svs.clients[i];
                    if( !host.HostClient.active )
                        continue;
                    if( common.SameText( host.HostClient.name, cmd.Argv( 1 ) ) )
                        break;
                }
            }

            if( i < server.svs.maxclients )
            {
                string who;
                if( cmd.Source == cmd_source_t.src_command )
                    if( QClient.cls.state == ServerType.DEDICATED )
                        who = "Console";
                    else
                        who = QClient.Name;
                else
                    who = save.name;

                // can't kick yourself!
                if( host.HostClient == save )
                    return;

                string message = null;
                if( cmd.Argc > 2 )
                {
                    message = common.Parse( cmd.Args );
                    if( byNumber )
                    {
                        message = message.Substring( 1 ); // skip the #
                        message = message.Trim(); // skip white space
                        message = message.Substring( cmd.Argv( 2 ).Length );	// skip the number
                    }
                    message = message.Trim();
                }
                if( !string.IsNullOrEmpty( message ) )
                    server.ClientPrint( "Kicked by {0}: {1}\n", who, message );
                else
                    server.ClientPrint( "Kicked by {0}\n", who );
                server.DropClient( false );
            }

            host.HostClient = save;
        }

        /// <summary>
        /// Host_Give_f
        /// </summary>
        private static void Give_f()
        {
            if( cmd.Source == cmd_source_t.src_command )
            {
                cmd.ForwardToServer();
                return;
            }

            if( progs.GlobalStruct.deathmatch != 0 && !host.HostClient.privileged )
                return;

            string t = cmd.Argv( 1 );
            int v = common.atoi( cmd.Argv( 2 ) );

            if( string.IsNullOrEmpty( t ) )
                return;

            switch( t[0] )
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    // MED 01/04/97 added hipnotic give stuff
                    if( common.GameKind == GameKind.Hipnotic )
                    {
                        if( t[0] == '6' )
                        {
                            if( t[1] == 'a' )
                                server.Player.v.items = (int)server.Player.v.items | QItems.HIT_PROXIMITY_GUN;
                            else
                                server.Player.v.items = (int)server.Player.v.items | QItems.IT_GRENADE_LAUNCHER;
                        }
                        else if( t[0] == '9' )
                            server.Player.v.items = (int)server.Player.v.items | QItems.HIT_LASER_CANNON;
                        else if( t[0] == '0' )
                            server.Player.v.items = (int)server.Player.v.items | QItems.HIT_MJOLNIR;
                        else if( t[0] >= '2' )
                            server.Player.v.items = (int)server.Player.v.items | ( QItems.IT_SHOTGUN << ( t[0] - '2' ) );
                    }
                    else
                    {
                        if( t[0] >= '2' )
                            server.Player.v.items = (int)server.Player.v.items | ( QItems.IT_SHOTGUN << ( t[0] - '2' ) );
                    }
                    break;

                case 's':
                    if( common.GameKind == GameKind.Rogue )
                        progs.SetEdictFieldFloat( server.Player, "ammo_shells1", v );

                    server.Player.v.ammo_shells = v;
                    break;

                case 'n':
                    if( common.GameKind == GameKind.Rogue )
                    {
                        if( progs.SetEdictFieldFloat( server.Player, "ammo_nails1", v ) )
                            if( server.Player.v.weapon <= QItems.IT_LIGHTNING )
                                server.Player.v.ammo_nails = v;
                    }
                    else
                        server.Player.v.ammo_nails = v;
                    break;

                case 'l':
                    if( common.GameKind == GameKind.Rogue )
                    {
                        if( progs.SetEdictFieldFloat( server.Player, "ammo_lava_nails", v ) )
                            if( server.Player.v.weapon > QItems.IT_LIGHTNING )
                                server.Player.v.ammo_nails = v;
                    }
                    break;

                case 'r':
                    if( common.GameKind == GameKind.Rogue )
                    {
                        if( progs.SetEdictFieldFloat( server.Player, "ammo_rockets1", v ) )
                            if( server.Player.v.weapon <= QItems.IT_LIGHTNING )
                                server.Player.v.ammo_rockets = v;
                    }
                    else
                    {
                        server.Player.v.ammo_rockets = v;
                    }
                    break;

                case 'm':
                    if( common.GameKind == GameKind.Rogue )
                    {
                        if( progs.SetEdictFieldFloat( server.Player, "ammo_multi_rockets", v ) )
                            if( server.Player.v.weapon > QItems.IT_LIGHTNING )
                                server.Player.v.ammo_rockets = v;
                    }
                    break;

                case 'h':
                    server.Player.v.health = v;
                    break;

                case 'c':
                    if( common.GameKind == GameKind.Rogue )
                    {
                        if( progs.SetEdictFieldFloat( server.Player, "ammo_cells1", v ) )
                            if( server.Player.v.weapon <= QItems.IT_LIGHTNING )
                                server.Player.v.ammo_cells = v;
                    }
                    else
                    {
                        server.Player.v.ammo_cells = v;
                    }
                    break;

                case 'p':
                    if( common.GameKind == GameKind.Rogue )
                    {
                        if( progs.SetEdictFieldFloat( server.Player, "ammo_plasma", v ) )
                            if( server.Player.v.weapon > QItems.IT_LIGHTNING )
                                server.Player.v.ammo_cells = v;
                    }
                    break;
            }
        }

        private static edict_t FindViewthing()
        {
            for( int i = 0; i < server.sv.num_edicts; i++ )
            {
                edict_t e = server.EdictNum( i );
                if( progs.GetString( e.v.classname ) == "viewthing" )
                    return e;
            }
            Con.Print( "No viewthing on map\n" );
            return null;
        }

        // Host_Viewmodel_f
        private static void Viewmodel_f()
        {
            edict_t e = FindViewthing();
            if( e == null )
                return;

            model_t m = Mod.ForName( cmd.Argv( 1 ), false );
            if( m == null )
            {
                Con.Print( "Can't load {0}\n", cmd.Argv( 1 ) );
                return;
            }

            e.v.frame = 0;
            QClient.cl.model_precache[(int)e.v.modelindex] = m;
        }

        /// <summary>
        /// Host_Viewframe_f
        /// </summary>
        private static void Viewframe_f()
        {
            edict_t e = FindViewthing();
            if( e == null )
                return;

            model_t m = QClient.cl.model_precache[(int)e.v.modelindex];

            int f = common.atoi( cmd.Argv( 1 ) );
            if( f >= m.numframes )
                f = m.numframes - 1;

            e.v.frame = f;
        }

        private static void PrintFrameName( model_t m, int frame )
        {
            aliashdr_t hdr = Mod.GetExtraData( m );
            if( hdr == null )
                return;

            Con.Print( "frame {0}: {1}\n", frame, hdr.frames[frame].name );
        }

        /// <summary>
        /// Host_Viewnext_f
        /// </summary>
        private static void Viewnext_f()
        {
            edict_t e = FindViewthing();
            if( e == null )
                return;

            model_t m = QClient.cl.model_precache[(int)e.v.modelindex];

            e.v.frame = e.v.frame + 1;
            if( e.v.frame >= m.numframes )
                e.v.frame = m.numframes - 1;

            PrintFrameName( m, (int)e.v.frame );
        }

        /// <summary>
        /// Host_Viewprev_f
        /// </summary>
        private static void Viewprev_f()
        {
            edict_t e = FindViewthing();
            if( e == null )
                return;

            model_t m = QClient.cl.model_precache[(int)e.v.modelindex];

            e.v.frame = e.v.frame - 1;
            if( e.v.frame < 0 )
                e.v.frame = 0;

            PrintFrameName( m, (int)e.v.frame );
        }

        // Host_Startdemos_f
        private static void Startdemos_f()
        {
            if( QClient.cls.state == ServerType.DEDICATED )
            {
                if( !server.sv.active )
                    Cbuf.AddText( "map start\n" );
                return;
            }

            int c = cmd.Argc - 1;
            if( c > QClient.MAX_DEMOS )
            {
                Con.Print( "Max {0} demos in demoloop\n", QClient.MAX_DEMOS );
                c = QClient.MAX_DEMOS;
            }
            Con.Print( "{0} demo(s) in loop\n", c );

            for( int i = 1; i < c + 1; i++ )
                QClient.cls.demos[i - 1] = common.Copy( cmd.Argv( i ), QClient.MAX_DEMONAME );

            if( !server.sv.active && QClient.cls.demonum != -1 && !QClient.cls.demoplayback )
            {
                QClient.cls.demonum = 0;
                QClient.NextDemo();
            }
            else
                QClient.cls.demonum = -1;
        }

        /// <summary>
        /// Host_Demos_f
        /// Return to looping demos
        /// </summary>
        private static void Demos_f()
        {
            if( QClient.cls.state == ServerType.DEDICATED )
                return;
            if( QClient.cls.demonum == -1 )
                QClient.cls.demonum = 1;
            QClient.Disconnect_f();
            QClient.NextDemo();
        }

        /// <summary>
        /// Host_Stopdemo_f
        /// Return to looping demos
        /// </summary>
        private static void Stopdemo_f()
        {
            if( QClient.cls.state == ServerType.DEDICATED )
                return;
            if( !QClient.cls.demoplayback )
                return;
            QClient.StopPlayback();
            QClient.Disconnect();
        }
    }
}
