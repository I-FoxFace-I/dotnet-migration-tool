"""
Internationalization (i18n) support for Migration Tool.
Supports English (default), Czech, Polish and Ukrainian UI translations.
"""

from .translations import TRANSLATIONS
from .utils import get_text, t, get_available_languages, get_language_display_name

__all__ = [
    "TRANSLATIONS",
    "get_text",
    "t",
    "get_available_languages",
    "get_language_display_name",
]
