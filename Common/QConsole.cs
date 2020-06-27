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
using System.IO;
using System.Text;

namespace SharpQuake
{
    /// <summary>
    /// Con_functions
    /// </summary>
    internal static class QConsole
    {
        public static bool IsInitialized => _IsInitialized;

        public static bool ForcedUp { get => _ForcedUp; set => _ForcedUp = value; }

        public static int NotifyLines { get => _NotifyLines; set => _NotifyLines = value; }

        public static int TotalLines => _TotalLines;

        public static int    BackScroll;
        private const string LOG_FILE_NAME = "console.log";

        private const int CON_TEXTSIZE  = 16384;
        private const int NUM_CON_TIMES = 4;

        private static char[] _Text = new char[CON_TEXTSIZE]; // char		*con_text=0;
        private static int    _VisLines;                      // con_vislines
        private static int    _TotalLines;                    // con_totallines   // total lines in console scrollback

        // con_backscroll		// lines up from bottom to display
        private static int _Current; // con_current		// where next message will be printed

        private static int      _X;                                 // con_x		// offset in current line for next print
        private static int      _CR;                                // from Print()
        private static double[] _Times = new double[NUM_CON_TIMES]; // con_times	// realtime time the line was generated

        // for transparent notify lines
        private static int _LineWidth; // con_linewidth

        private static bool       _DebugLog;        // qboolean	con_debuglog;
        private static bool       _IsInitialized;   // qboolean con_initialized;
        private static bool       _ForcedUp;        // qboolean con_forcedup		// because no entities to refresh
        private static int        _NotifyLines;     // con_notifylines	// scan lines to clear for notify lines
        private static QCVar       _NotifyTime;      // con_notifytime = { "con_notifytime", "3" };		//seconds
        private static float      _CursorSpeed = 4; // con_cursorspeed
        private static FileStream _Log;

        // Con_CheckResize (void)
        public static void CheckResize()
        {
            int width = ( Scr.vid.width >> 3 ) - 2;
            if( width == _LineWidth )
                return;

            if( width < 1 ) // video hasn't been initialized yet
            {
                width       = 38;
                _LineWidth  = width; // con_linewidth = width;
                _TotalLines = CON_TEXTSIZE / _LineWidth;
                QCommon.FillArray( _Text, ' ' ); // Q_memset (con_text, ' ', CON_TEXTSIZE);
            }
            else
            {
                int oldwidth = _LineWidth;
                _LineWidth = width;
                int oldtotallines = _TotalLines;
                _TotalLines = CON_TEXTSIZE / _LineWidth;
                int numlines = oldtotallines;

                if( _TotalLines < numlines )
                    numlines = _TotalLines;

                int numchars = oldwidth;

                if( _LineWidth < numchars )
                    numchars = _LineWidth;

                char[] tmp = _Text;
                _Text = new char[CON_TEXTSIZE];
                QCommon.FillArray( _Text, ' ' );

                for( int i = 0; i < numlines; i++ )
                {
                    for( int j = 0; j < numchars; j++ )
                    {
                        _Text[( _TotalLines - 1 - i ) * _LineWidth + j] = tmp[( ( _Current - i + oldtotallines ) %
                                                                                oldtotallines ) * oldwidth + j];
                    }
                }

                ClearNotify();
            }

            BackScroll = 0;
            _Current   = _TotalLines - 1;
        }

        // Con_Init (void)
        public static void Init()
        {
            _DebugLog = ( QCommon.CheckParm( "-condebug" ) > 0 );
            if( _DebugLog )
            {
                string path = Path.Combine( QCommon.GameDir, LOG_FILE_NAME );
                if( File.Exists( path ) )
                    File.Delete( path );

                _Log = new FileStream( path, FileMode.Create, FileAccess.Write, FileShare.Read );
            }

            _LineWidth = -1;
            CheckResize();

            QConsole.Print( "Console initialized.\n" );

            //
            // register our commands
            //
            if( _NotifyTime == null )
            {
                _NotifyTime = new QCVar( "con_notifytime", "3" );
            }

            QCommand.Add( "toggleconsole", ToggleConsole_f );
            QCommand.Add( "messagemode",   MessageMode_f );
            QCommand.Add( "messagemode2",  MessageMode2_f );
            QCommand.Add( "clear",         Clear_f );

            _IsInitialized = true;
        }

        // Con_DrawConsole
        //
        // Draws the console with the solid background
        // The typing QInput line at the bottom should only be drawn if typing is allowed
        public static void Draw( int lines, bool drawinput )
        {
            if( lines <= 0 )
                return;

            // draw the background
            QGLDraw.DrawConsoleBackground( lines );

            // draw the text
            _VisLines = lines;

            int rows = ( lines - 16 ) >> 3;        // rows of text to draw
            int y    = lines - 16 - ( rows << 3 ); // may start slightly negative

            for( int i = _Current - rows + 1; i <= _Current; i++, y += 8 )
            {
                int j = i - BackScroll;
                if( j < 0 )
                    j = 0;

                int offset = ( j % _TotalLines ) * _LineWidth;

                for( int x = 0; x < _LineWidth; x++ )
                    QGLDraw.DrawCharacter( ( x + 1 ) << 3, y, _Text[offset + x] );
            }

            // draw the QInput prompt, user text, and cursor if desired
            if( drawinput )
                DrawInput();
        }

        /// <summary>
        /// Con_Printf
        /// </summary>
        public static void Print( string fmt, params object[] args )
        {
            string msg = ( args.Length > 0 ? string.Format( fmt, args ) : fmt );

            Console.WriteLine( msg ); // Debug stuff

            // log all messages to file
            if( _DebugLog )
                DebugLog( msg );

            if( !_IsInitialized )
                return;

            if( QClient.cls.state == QServerType.DEDICATED )
                return; // no graphics mode

            // write it to the scrollable buffer
            Print( msg );

            // update the screen if the console is displayed
            if( QClient.cls.signon != QClient.SIGNONS && !Scr.IsDisabledForLoading )
                Scr.UpdateScreen();
        }

        public static void Shutdown()
        {
            if( _Log != null )
            {
                _Log.Flush();
                _Log.Dispose();
                _Log = null;
            }
        }

        //
        // Con_DPrintf
        //
        // A Con_Printf that only shows up if the "developer" QCVar is set
        public static void DPrint( string fmt, params object[] args )
        {
            // don't confuse non-developers with techie stuff...
            if( QHost.IsDeveloper )
                Print( fmt, args );
        }

        // Con_SafePrintf (char *fmt, ...)
        //
        // Okay to call even when the screen can't be updated
        public static void SafePrint( string fmt, params object[] args )
        {
            bool temp = Scr.IsDisabledForLoading;
            Scr.IsDisabledForLoading = true;
            Print( fmt, args );
            Scr.IsDisabledForLoading = temp;
        }

        /// <summary>
        /// Con_DrawNotify
        /// </summary>
        public static void DrawNotify()
        {
            int v = 0;
            for( int i = _Current - NUM_CON_TIMES + 1; i <= _Current; i++ )
            {
                if( i < 0 )
                    continue;
                double time = _Times[i % NUM_CON_TIMES];
                if( Math.Abs( time ) < 0.001f )
                    continue;
                time = QHost.RealTime - time;
                if( time > _NotifyTime.Value )
                    continue;

                int textOffset = ( i % _TotalLines ) * _LineWidth;

                Scr.ClearNotify = 0;
                Scr.CopyTop     = true;

                for( int x = 0; x < _LineWidth; x++ )
                    QGLDraw.DrawCharacter( ( x + 1 ) << 3, v, _Text[textOffset + x] );

                v += 8;
            }

            if( QKey.Destination == QKeyDest.Message )
            {
                Scr.ClearNotify = 0;
                Scr.CopyTop     = true;

                int x = 0;

                QGLDraw.DrawString( 8, v, "say:" );
                string chat = QKey.ChatBuffer;
                for( ; x < chat.Length; x++ )
                {
                    QGLDraw.DrawCharacter( ( x + 5 ) << 3, v, chat[x] );
                }

                QGLDraw.DrawCharacter( ( x + 5 ) << 3, v, 10 + ( (int) ( QHost.RealTime * _CursorSpeed ) & 1 ) );
                v += 8;
            }

            if( v > _NotifyLines )
                _NotifyLines = v;
        }

        // Con_ClearNotify (void)
        public static void ClearNotify()
        {
            for( int i = 0; i < NUM_CON_TIMES; i++ )
                _Times[i] = 0;
        }

        /// <summary>
        /// Con_ToggleConsole_f
        /// </summary>
        public static void ToggleConsole_f()
        {
            if( QKey.Destination == QKeyDest.Console )
            {
                if( QClient.cls.state == QServerType.CONNECTED )
                {
                    QKey.Destination            = QKeyDest.Game;
                    QKey.Lines[QKey.EditLine][1] = '\0'; // clear any typing
                    QKey.LinePos                = 1;
                }
                else
                {
                    MenuBase.MainMenu.Show();
                }
            }
            else
                QKey.Destination = QKeyDest.Console;

            Scr.EndLoadingPlaque();
            Array.Clear( _Times, 0, _Times.Length );
        }

        /// <summary>
        /// Con_DebugLog
        /// </summary>
        private static void DebugLog( string msg )
        {
            if( _Log != null )
            {
                byte[] tmp = Encoding.UTF8.GetBytes( msg );
                _Log.Write( tmp, 0, tmp.Length );
            }
        }

        // Con_Print (char *txt)
        //
        // Handles cursor positioning, line wrapping, etc
        // All console printing must go through this in order to be logged to disk
        // If no console is visible, the notify window will pop up.
        private static void Print( string txt )
        {
            if( string.IsNullOrEmpty( txt ) )
                return;

            int mask, offset = 0;

            BackScroll = 0;

            if( txt.StartsWith( ( (char) 1 ).ToString() ) ) // [0] == 1)
            {
                mask = 128;                           // go to colored text
                QSound.LocalSound( "misc/talk.wav" ); // play talk wav
                offset++;
            }
            else if( txt.StartsWith( ( (char) 2 ).ToString() ) ) //txt[0] == 2)
            {
                mask = 128; // go to colored text
                offset++;
            }
            else
                mask = 0;

            while( offset < txt.Length )
            {
                char c = txt[offset];

                int l;
                // count word length
                for( l = 0; l < _LineWidth && offset + l < txt.Length; l++ )
                {
                    if( txt[offset + l] <= ' ' )
                        break;
                }

                // word wrap
                if( l != _LineWidth && ( _X + l > _LineWidth ) )
                    _X = 0;

                offset++;

                if( _CR != 0 )
                {
                    _Current--;
                    _CR = 0;
                }

                if( _X == 0 )
                {
                    LineFeed();
                    // mark time for transparent overlay
                    if( _Current >= 0 )
                        _Times[_Current % NUM_CON_TIMES] = QHost.RealTime; // realtime
                }

                switch( c )
                {
                    case '\n':
                        _X = 0;
                        break;

                    case '\r':
                        _X  = 0;
                        _CR = 1;
                        break;

                    default: // display character and advance
                        int y = _Current % _TotalLines;
                        _Text[y * _LineWidth + _X] = (char) ( c | mask );
                        _X++;
                        if( _X >= _LineWidth )
                            _X = 0;
                        break;
                }
            }
        }

        /// <summary>
        /// Con_Clear_f
        /// </summary>
        private static void Clear_f()
        {
            QCommon.FillArray( _Text, ' ' );
        }

        // Con_MessageMode_f
        private static void MessageMode_f()
        {
            QKey.Destination = QKeyDest.Message;
            QKey.TeamMessage = false;
        }

        //Con_MessageMode2_f
        private static void MessageMode2_f()
        {
            QKey.Destination = QKeyDest.Message;
            QKey.TeamMessage = true;
        }

        // Con_Linefeed
        private static void LineFeed()
        {
            _X = 0;
            _Current++;

            for( int i = 0; i < _LineWidth; i++ )
            {
                _Text[( _Current % _TotalLines ) * _LineWidth + i] = ' ';
            }
        }

        // Con_DrawInput
        //
        // The QInput line scrolls horizontally if typing goes beyond the right edge
        private static void DrawInput()
        {
            if( QKey.Destination != QKeyDest.Console && !_ForcedUp )
                return; // don't draw anything

            // add the cursor frame
            QKey.Lines[QKey.EditLine][QKey.LinePos] = (char) ( 10 + ( (int) ( QHost.RealTime * _CursorSpeed ) & 1 ) );

            // fill out remainder with spaces
            for( int i = QKey.LinePos + 1; i < _LineWidth; i++ )
                QKey.Lines[QKey.EditLine][i] = ' ';

            //	prestep if horizontally scrolling
            int offset = 0;
            if( QKey.LinePos >= _LineWidth )
                offset = 1 + QKey.LinePos - _LineWidth;
            //text += 1 + key_linepos - con_linewidth;

            // draw it
            int y = _VisLines - 16;

            for( int i = 0; i < _LineWidth; i++ )
                QGLDraw.DrawCharacter( ( i + 1 ) << 3, _VisLines - 16, QKey.Lines[QKey.EditLine][offset + i] );

            // remove cursor
            QKey.Lines[QKey.EditLine][QKey.LinePos] = '\0';
        }
    }
}
