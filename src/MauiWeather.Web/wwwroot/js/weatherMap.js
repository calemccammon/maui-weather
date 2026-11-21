// The map, drawn with MapLibre GL JS (loaded by index.html as `maplibregl`).
// WeatherMap.razor owns one of these and tells it what to show.

/** OpenFreeMap's hosted styles: free, no key, and no request limit. */
const styleUrl = {
  light: 'https://tiles.openfreemap.org/styles/liberty',
  dark: 'https://tiles.openfreemap.org/styles/dark',
}

const view = {
  initialCenter: [0, 25],
  initialZoom: 1.5,
  minZoom: 1,
  maxZoom: 18,
  /** How far in the map goes when a place is chosen from further out. */
  placeZoom: 10,
  moveDurationMs: 600,
}

/** How long a press must be held, and how far it may wander, to count. */
const longPressMs = 500
const longPressSlopPx = 8

/**
 * Draws a map in `container`. A press held in one spot is reported to
 * `listener` as the point under it.
 */
export function create(container, listener, dark) {
  const map = new maplibregl.Map({
    container,
    style: dark ? styleUrl.dark : styleUrl.light,
    center: view.initialCenter,
    zoom: view.initialZoom,
    minZoom: view.minZoom,
    maxZoom: view.maxZoom,
    dragRotate: false,
    pitchWithRotate: false,
    touchPitch: false,
    // Added below, written out in full: the credit the map's makers ask for.
    attributionControl: false,
  })
  map.addControl(new maplibregl.AttributionControl({ compact: false }), 'bottom-right')
  map.touchZoomRotate.disableRotation()
  watchForLongPress(map, ({ lat, lng }) => listener.invokeMethodAsync('OnLongPress', lat, lng))

  let marker
  let shown = { pin: null, covered: null, dark }

  function showPin(pin) {
    marker?.remove()
    marker = undefined
    if (pin === null) return

    // The pin is in the theme's colour. The default marker is a pin whose
    // tip sits on the point.
    const color = getComputedStyle(container).getPropertyValue('--color-primary').trim()
    marker = new maplibregl.Marker({ color }).setLngLat([pin.longitude, pin.latitude]).addTo(map)
  }

  function moveTo(pin, covered) {
    if (pin === null) return
    map.easeTo({
      center: [pin.longitude, pin.latitude],
      zoom: Math.max(map.getZoom(), view.placeZoom),
      padding: covered,
      duration: view.moveDurationMs,
    })
  }

  return {
    /**
     * Shows `pin` (or none, when null), clear of the parts of the map that
     * `covered` says something else is over, in the light or dark style.
     */
    show(pin, covered, dark) {
      const pinMoved = !samePin(pin, shown.pin)
      const coverChanged = !sameInsets(covered, shown.covered)
      const themeChanged = dark !== shown.dark
      shown = { pin, covered, dark }

      if (themeChanged) map.setStyle(dark ? styleUrl.dark : styleUrl.light)
      if (pinMoved || themeChanged) showPin(pin)
      // Selecting a place, or a change in what covers the map, moves the
      // map so the pin sits in the clear.
      if (pinMoved || coverChanged) moveTo(pin, covered)
    },

    dispose() {
      map.remove()
    },
  }
}

function samePin(a, b) {
  if (a === null || b === null) return a === b
  return a.latitude === b.latitude && a.longitude === b.longitude
}

function sameInsets(a, b) {
  if (a === null || b === null) return a === b
  return a.top === b.top && a.right === b.right && a.bottom === b.bottom && a.left === b.left
}

/**
 * MapLibre GL JS has no long-press event, so one is made from a press that
 * is held in one spot.
 */
function watchForLongPress(map, onLongPress) {
  let timer
  let start

  const cancel = () => {
    clearTimeout(timer)
    start = undefined
  }
  const begin = (event) => {
    cancel()
    // A second finger means a pinch, not a press.
    if ('points' in event && event.points.length > 1) return
    start = event.point
    timer = setTimeout(() => onLongPress(event.lngLat), longPressMs)
  }
  const moved = (event) => {
    if (start !== undefined && start.dist(event.point) > longPressSlopPx) cancel()
  }

  map.on('mousedown', begin)
  map.on('touchstart', begin)
  map.on('mousemove', moved)
  map.on('touchmove', moved)
  map.on('mouseup', cancel)
  map.on('touchend', cancel)
  map.on('touchcancel', cancel)
  map.on('movestart', cancel)
  // A held press must not also open the browser's own menu.
  map.on('contextmenu', (event) => event.preventDefault())
}
