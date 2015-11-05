/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/
using System;
using System.Windows.Forms;

namespace QuantConnect.Views
{
    public static class Extensions
    {
        /// <summary>
        /// Execute a method on the control's owning thread.
        /// </summary>
        /// <param name="uiElement">The control that is being updated.</param>
        /// <param name="updater">The method that updates uiElement.</param>
        /// <param name="forceSynchronous">True to force synchronous execution of 
        /// updater.  False to allow asynchronous execution if the call is marshalled
        /// from a non-GUI thread.  If the method is called on the GUI thread,
        /// execution is always synchronous.</param>
        public static void SafeInvoke(this Control uiElement, Action updater, bool forceSynchronous = true)
        {
            if (uiElement == null)
            {
                throw new ArgumentNullException("uiElement");
            }

            if (uiElement.InvokeRequired)
            {
                if (forceSynchronous)
                {
                    uiElement.Invoke((Action)delegate { SafeInvoke(uiElement, updater, forceSynchronous); });
                }
                else
                {
                    uiElement.BeginInvoke((Action)delegate { SafeInvoke(uiElement, updater, forceSynchronous); });
                }
            }
            else
            {
                if (!uiElement.IsHandleCreated)
                {
                    // Do nothing if the handle isn't created already.  The user's responsible
                    // for ensuring that the handle they give us exists.
                    return;
                }

                if (uiElement.IsDisposed)
                {
                    throw new ObjectDisposedException("Control is already disposed.");
                }

                updater();
            }
        }
    }
}
