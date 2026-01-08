"""
i18n utility functions for Migration Tool.
"""

from typing import List, Dict

from .translations import TRANSLATIONS


# Language display names
LANGUAGE_NAMES: Dict[str, str] = {
    "en": "🇬🇧 English",
    "cs": "🇨🇿 Čeština",
    "pl": "🇵🇱 Polski",
    "uk": "🇺🇦 Українська",
}


def get_text(key: str, lang: str = "en", **kwargs) -> str:
    """
    Get translated text for a given key.
    
    Args:
        key: Translation key
        lang: Language code ('en', 'cs', 'pl', 'uk')
        **kwargs: Format arguments for the string
    
    Returns:
        Translated string, or key if not found
    """
    translations = TRANSLATIONS.get(lang, TRANSLATIONS["en"])
    text = translations.get(key, TRANSLATIONS["en"].get(key, key))
    
    if kwargs:
        try:
            text = text.format(**kwargs)
        except KeyError:
            pass
    
    return text


def t(key: str, **kwargs) -> str:
    """
    Shorthand for get_text using current UI language from session state.
    Must be called after session state is initialized.
    """
    import streamlit as st
    lang = st.session_state.get("ui_language", "en")
    return get_text(key, lang, **kwargs)


def get_available_languages() -> List[str]:
    """Get list of available UI language codes."""
    return list(TRANSLATIONS.keys())


def get_language_display_name(code: str) -> str:
    """Get display name for a language code."""
    return LANGUAGE_NAMES.get(code, code)


def get_language_options() -> Dict[str, str]:
    """Get dictionary of language code -> display name for UI selection."""
    return {code: LANGUAGE_NAMES.get(code, code) for code in TRANSLATIONS.keys()}
