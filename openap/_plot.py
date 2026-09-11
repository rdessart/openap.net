"""Load the optional plotting dependency only when needed."""


def get_pyplot():
    """Return pyplot, with an install instruction if Matplotlib is absent."""
    try:
        from matplotlib import pyplot
    except ModuleNotFoundError as exc:
        if exc.name != "matplotlib":
            raise
        raise ImportError(
            'Plotting requires Matplotlib. Install it with: pip install "openap[plot]"'
        ) from exc
    return pyplot
