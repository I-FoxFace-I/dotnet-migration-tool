"""
Tests for i18n module.
"""

import sys
from pathlib import Path

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))


def test_translations_structure():
    """Test that all languages have the same keys."""
    print("\n--- test_translations_structure ---")
    
    from i18n.translations import TRANSLATIONS
    
    # Get English keys as reference
    en_keys = set(TRANSLATIONS["en"].keys())
    print(f"  English has {len(en_keys)} translation keys")
    
    # Check all languages
    languages = list(TRANSLATIONS.keys())
    print(f"  Languages: {languages}")
    
    all_match = True
    for lang in languages:
        lang_keys = set(TRANSLATIONS[lang].keys())
        
        missing = en_keys - lang_keys
        extra = lang_keys - en_keys
        
        if missing:
            print(f"  ⚠️ {lang}: Missing keys: {missing}")
            all_match = False
        
        if extra:
            print(f"  ⚠️ {lang}: Extra keys: {extra}")
            all_match = False
    
    assert all_match, "Not all languages have the same keys"
    print("✅ test_translations_structure PASSED")
    return True


def test_get_text():
    """Test get_text function."""
    print("\n--- test_get_text ---")
    
    from i18n.utils import get_text
    
    # Test basic retrieval
    text = get_text("app_title", "en")
    assert "Migration Tool" in text, f"Expected 'Migration Tool' in '{text}'"
    print(f"  English title: {text}")
    
    # Test Czech
    text_cs = get_text("app_title", "cs")
    assert "Migrační" in text_cs, f"Expected 'Migrační' in '{text_cs}'"
    print(f"  Czech title: {text_cs}")
    
    # Test Polish
    text_pl = get_text("app_title", "pl")
    assert "migracji" in text_pl, f"Expected 'migracji' in '{text_pl}'"
    print(f"  Polish title: {text_pl}")
    
    # Test Ukrainian
    text_uk = get_text("app_title", "uk")
    assert "міграції" in text_uk, f"Expected 'міграції' in '{text_uk}'"
    print(f"  Ukrainian title: {text_uk}")
    
    # Test fallback to English for unknown language
    text_unknown = get_text("app_title", "xx")
    assert text_unknown == text, "Should fallback to English"
    print(f"  Unknown lang fallback: {text_unknown}")
    
    # Test fallback for unknown key
    text_missing = get_text("nonexistent_key", "en")
    assert text_missing == "nonexistent_key", "Should return key for unknown translation"
    print(f"  Missing key fallback: {text_missing}")
    
    print("✅ test_get_text PASSED")
    return True


def test_get_text_with_params():
    """Test get_text with format parameters."""
    print("\n--- test_get_text_with_params ---")
    
    from i18n.utils import get_text
    
    # Test with count parameter
    text = get_text("files_count", "en", count=42)
    assert "42" in text, f"Expected '42' in '{text}'"
    print(f"  With count: {text}")
    
    # Test error message with error param
    text_error = get_text("error_loading_solution", "en", error="Test error")
    assert "Test error" in text_error, f"Expected 'Test error' in '{text_error}'"
    print(f"  With error: {text_error}")
    
    print("✅ test_get_text_with_params PASSED")
    return True


def test_available_languages():
    """Test get_available_languages function."""
    print("\n--- test_available_languages ---")
    
    from i18n.utils import get_available_languages
    
    languages = get_available_languages()
    print(f"  Available languages: {languages}")
    
    assert "en" in languages, "English should be available"
    assert "cs" in languages, "Czech should be available"
    assert "pl" in languages, "Polish should be available"
    assert "uk" in languages, "Ukrainian should be available"
    assert len(languages) == 4, f"Expected 4 languages, got {len(languages)}"
    
    print("✅ test_available_languages PASSED")
    return True


def test_language_display_names():
    """Test language display names."""
    print("\n--- test_language_display_names ---")
    
    from i18n.utils import get_language_display_name
    
    # Test each language
    assert "English" in get_language_display_name("en")
    assert "Čeština" in get_language_display_name("cs")
    assert "Polski" in get_language_display_name("pl")
    assert "Українська" in get_language_display_name("uk")
    
    print(f"  en: {get_language_display_name('en')}")
    print(f"  cs: {get_language_display_name('cs')}")
    print(f"  pl: {get_language_display_name('pl')}")
    print(f"  uk: {get_language_display_name('uk')}")
    
    # Unknown language should return code
    assert get_language_display_name("xx") == "xx"
    
    print("✅ test_language_display_names PASSED")
    return True


def test_all_translations_non_empty():
    """Test that all translations are non-empty strings."""
    print("\n--- test_all_translations_non_empty ---")
    
    from i18n.translations import TRANSLATIONS
    
    empty_found = []
    
    for lang, translations in TRANSLATIONS.items():
        for key, value in translations.items():
            if not value or not isinstance(value, str):
                empty_found.append((lang, key, value))
    
    if empty_found:
        for lang, key, value in empty_found:
            print(f"  ⚠️ Empty/invalid: {lang}.{key} = {repr(value)}")
    
    assert len(empty_found) == 0, f"Found {len(empty_found)} empty/invalid translations"
    
    print(f"  All translations are non-empty strings")
    print("✅ test_all_translations_non_empty PASSED")
    return True


def run_all_tests():
    """Run all i18n tests."""
    print("=" * 60)
    print("Running i18n Tests")
    print("=" * 60)
    
    tests = [
        test_translations_structure,
        test_get_text,
        test_get_text_with_params,
        test_available_languages,
        test_language_display_names,
        test_all_translations_non_empty,
    ]
    
    passed = 0
    failed = 0
    
    for test in tests:
        try:
            if test():
                passed += 1
            else:
                failed += 1
        except Exception as e:
            print(f"❌ {test.__name__} FAILED with exception: {e}")
            import traceback
            traceback.print_exc()
            failed += 1
    
    print("\n" + "=" * 60)
    print(f"Results: {passed} passed, {failed} failed")
    print("=" * 60)
    
    return failed == 0


if __name__ == "__main__":
    success = run_all_tests()
    sys.exit(0 if success else 1)
