﻿/* Rewritten in C# by Yury Kiselev, 2010.
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

namespace SharpQuake
{
    internal static class BSPLumpFlag
    {
        public const int LUMP_ENTITIES     = 0;
        public const int LUMP_PLANES       = 1;
        public const int LUMP_TEXTURES     = 2;
        public const int LUMP_VERTEXES     = 3;
        public const int LUMP_VISIBILITY   = 4;
        public const int LUMP_NODES        = 5;
        public const int LUMP_TEXINFO      = 6;
        public const int LUMP_FACES        = 7;
        public const int LUMP_LIGHTING     = 8;
        public const int LUMP_CLIPNODES    = 9;
        public const int LUMP_LEAFS        = 10;
        public const int LUMP_MARKSURFACES = 11;
        public const int LUMP_EDGES        = 12;
        public const int LUMP_SURFEDGES    = 13;
        public const int LUMP_MODELS       = 14;
    }
}
