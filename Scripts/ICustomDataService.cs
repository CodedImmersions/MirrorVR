/*
* MIT License
* Copyright (c) 2025-2026 Coded Immersions
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;
using System.Collections.Generic;

namespace Mirror.VR
{
    public interface ICustomDataService
    {
        //Getting/Setting data

        /// <summary>
        /// Sets a custom player data value to the key and value provided.
        /// </summary>
        /// <param name="key">The key to add/modify</param>
        /// <param name="value">The value to add/modify</param>
        public abstract void SetValue(string key, string value);

        /// <summary>
        /// Attempts to get the specified value in the database from the player, based on the key provided.
        /// </summary>
        /// <param name="key">The key to attempt to retrieve.</param>
        /// <param name="callback">The callback that returns the info. The first param (<see langword="bool"/>) returns <see langword="true"/> if it was a success, <see langword="false"/> otherwise. The second param (<see langword="string"/>) is the value, otherwise <see langword="null"/> if not found or an error occurred.</param>
        public abstract void TryGetValue(string key, Action<bool, string> callback);

        /// <summary>
        /// Attempts to get the specified global value in the database, based on the key provided.
        /// </summary>
        /// <param name="key">The key to attempt to retrieve.</param>
        /// <param name="callback">The callback that returns the info. The first param (<see langword="bool"/>) returns <see langword="true"/> if it was a success, <see langword="false"/> otherwise. The second param (<see langword="string"/>) is the value, otherwise <see langword="null"/> if not found or an error occurred.</param>
        public abstract void TryGetGlobalValue(string key, Action<bool, string> callback);



        //Cosmetics management
        //TODO: finish

        /// <summary>
        /// Gets the player's inventory to be used for equipping cosmetics.
        /// </summary>
        /// <param name="callback">The callback that returns the info.</param>
        public abstract void GetInventory(Action<List<Cosmetic>> callback);

        /// <summary>
        /// Gets the players equipped cosmetics.
        /// </summary>
        /// <param name="callback"></param>
        public abstract void GetEquippedCosmetics(Action<List<Cosmetic>, bool> callback);
        public abstract void SetEquippedCosmetics(List<Cosmetic> cosmetics);
        public abstract void CosmeticOwned(Cosmetic cosmetic, Action<bool> callback);
    }
}
