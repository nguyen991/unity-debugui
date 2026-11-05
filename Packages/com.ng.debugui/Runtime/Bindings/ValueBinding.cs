using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ng.DebugUI
{
    /// <summary>
    /// Binds a getter/setter pair to a UI element. The getter is polled by <see cref="Refresh"/>
    /// and the UI is only touched when the value changes. Exceptions are caught, flagged with
    /// <see cref="UssClasses.Error"/> and logged once per binding.
    /// </summary>
    internal sealed class ValueBinding<T> : IBinding
    {
        static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        readonly VisualElement element;
        readonly Func<T> getter;
        readonly Action<T> setter;
        readonly Action<T> applyToUI;

        T last;
        bool hasValue;
        bool logged;
        bool getterFailed;

        public ValueBinding(VisualElement element, Func<T> getter, Action<T> setter, Action<T> applyToUI)
        {
            this.element = element;
            this.getter = getter;
            this.setter = setter;
            this.applyToUI = applyToUI;
        }

        /// <summary>True while the user is interacting with the control; refresh is skipped.</summary>
        public bool IsEditing { get; set; }

        public bool HasError { get; private set; }

        public void Refresh()
        {
            if (IsEditing || getter == null)
                return;

            T value;
            try
            {
                value = getter();
            }
            catch (Exception e)
            {
                getterFailed = true;
                MarkError(e);
                return;
            }

            if (getterFailed)
            {
                // Recovered: push the value again so the error state is re-evaluated.
                getterFailed = false;
                hasValue = false;
            }

            if (hasValue && Comparer.Equals(last, value))
                return;

            last = value;
            hasValue = true;
            try
            {
                applyToUI(value);
                ClearError();
            }
            catch (Exception e)
            {
                // e.g. a ToString() that throws: keep it to this control.
                MarkError(e);
            }
        }

        public void Invalidate() => hasValue = false;

        /// <summary>Writes a value coming from the UI into the game via the setter.</summary>
        public void Write(T value)
        {
            if (setter == null)
                return;
            try
            {
                setter(value);
            }
            catch (Exception e)
            {
                MarkError(e);
            }
            // The setter may reject or clamp the input: re-read the game value on the next
            // refresh (once the user stops editing) so the UI never shows a value the game lacks.
            Invalidate();
        }

        public void MarkError(Exception e)
        {
            if (!HasError)
            {
                HasError = true;
                element.AddToClassList(UssClasses.Error);
            }
            if (!logged)
            {
                logged = true;
                Debug.LogException(e);
            }
        }

        void ClearError()
        {
            if (!HasError)
                return;
            HasError = false;
            element.RemoveFromClassList(UssClasses.Error);
        }
    }
}
