// The few things the views need of the browser that Blazor does not offer.

const darkScheme = window.matchMedia('(prefers-color-scheme: dark)')

/**
 * Tells `listener` the window's size and whether the system theme is dark:
 * once now, and again whenever either changes.
 */
export function watchWindow(listener) {
  const report = () =>
    listener.invokeMethodAsync('OnWindowChanged', window.innerWidth, window.innerHeight, darkScheme.matches)

  window.addEventListener('resize', report)
  darkScheme.addEventListener('change', report)
  report()

  return {
    stop() {
      window.removeEventListener('resize', report)
      darkScheme.removeEventListener('change', report)
    },
  }
}

/** Takes the keyboard away from `element`. */
export function blur(element) {
  element.blur()
}

/**
 * Keeps a drag that began on `element` reporting to it, wherever the pointer
 * goes next.
 */
export function capturePointer(element, pointerId) {
  // The pointer may already have been lifted.
  if (element.isConnected) {
    try {
      element.setPointerCapture(pointerId)
    } catch {
      // No such pointer any more: the drag is over.
    }
  }
}
