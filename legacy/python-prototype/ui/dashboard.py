"""
Dashboard component for Migration Tool.

Displays solution overview and metrics.
"""

import streamlit as st
from pathlib import Path
from collections import Counter

import sys
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from i18n import t
from core.file_scanner import FileScanner
from utils.logging_config import get_logger

logger = get_logger('ui.dashboard')


def render_dashboard():
    """Render the dashboard view with solution overview."""
    st.markdown(f"# {t('dashboard_title')}")
    
    solution = st.session_state.get("solution")
    
    if not solution:
        st.info(t("dashboard_no_solution"))
        return
    
    # Main metrics row
    _render_metrics(solution)
    
    st.markdown("---")
    
    # Two columns layout
    col1, col2 = st.columns(2)
    
    with col1:
        _render_project_types(solution)
    
    with col2:
        _render_dependencies(solution)
    
    st.markdown("---")
    
    # Project list
    _render_project_list(solution)


def _render_metrics(solution):
    """Render main metrics cards."""
    projects = solution.projects
    
    # Count project types
    test_projects = [p for p in projects if p.is_test_project]
    source_projects = [p for p in projects if not p.is_test_project]
    
    # Create metrics row
    col1, col2, col3, col4 = st.columns(4)
    
    with col1:
        st.metric(
            t("metric_projects"),
            len(projects)
        )
    
    with col2:
        st.metric(
            t("metric_source_projects"),
            len(source_projects)
        )
    
    with col3:
        st.metric(
            t("metric_test_projects"),
            len(test_projects)
        )
    
    with col4:
        # Count total files (if scanned)
        total_files = sum(len(getattr(p, 'files', [])) for p in projects)
        st.metric(
            t("metric_files"),
            total_files if total_files > 0 else "—"
        )


def _render_project_types(solution):
    """Render project types breakdown."""
    st.markdown(f"### {t('project_types')}")
    
    projects = solution.projects
    
    # Count by project type
    type_counts = Counter()
    for p in projects:
        ptype = p.project_type.value if hasattr(p, 'project_type') else 'other'
        type_counts[ptype] += 1
    
    # Map to display names
    type_names = {
        'classlib': t('type_library'),
        'console': t('type_console'),
        'wpf': t('type_wpf'),
        'test': t('type_test'),
        'web': t('type_web'),
        'other': t('type_other'),
    }
    
    # Display as table
    if type_counts:
        data = []
        for ptype, count in sorted(type_counts.items(), key=lambda x: -x[1]):
            display_name = type_names.get(ptype, ptype)
            data.append({"Type": display_name, "Count": count})
        
        st.dataframe(
            data,
            hide_index=True,
            use_container_width=True
        )
    else:
        st.info(t("no_files"))


def _render_dependencies(solution):
    """Render dependencies overview."""
    st.markdown(f"### {t('dependencies_title')}")
    
    projects = solution.projects
    
    # Count references
    project_refs = set()
    package_refs = set()
    
    for p in projects:
        if hasattr(p, 'project_references'):
            for ref in p.project_references:
                project_refs.add(ref.name if hasattr(ref, 'name') else str(ref))
        
        if hasattr(p, 'package_references'):
            for ref in p.package_references:
                package_refs.add(ref.name if hasattr(ref, 'name') else str(ref))
    
    col1, col2 = st.columns(2)
    
    with col1:
        st.metric(t("project_references"), len(project_refs))
    
    with col2:
        st.metric(t("package_references"), len(package_refs))
    
    # Show top packages
    if package_refs:
        with st.expander(f"📦 {t('package_references')} ({len(package_refs)})"):
            for pkg in sorted(package_refs)[:20]:
                st.text(f"• {pkg}")
            if len(package_refs) > 20:
                st.text(f"... and {len(package_refs) - 20} more")


def _render_project_list(solution):
    """Render list of all projects."""
    st.markdown(f"### 📦 {t('metric_projects')}")
    
    projects = solution.projects
    
    # Filter options
    col1, col2, col3 = st.columns([2, 1, 1])
    
    with col1:
        search = st.text_input(
            t("search"),
            placeholder=t("explorer_filter"),
            key="dashboard_search"
        )
    
    with col2:
        show_tests = st.checkbox(t("explorer_show_tests"), value=True, key="dash_show_tests")
    
    with col3:
        show_source = st.checkbox(t("explorer_show_source"), value=True, key="dash_show_source")
    
    # Filter projects
    filtered = []
    for p in projects:
        # Search filter
        if search and search.lower() not in p.name.lower():
            continue
        
        # Type filter
        if p.is_test_project and not show_tests:
            continue
        if not p.is_test_project and not show_source:
            continue
        
        filtered.append(p)
    
    # Display as table
    if filtered:
        data = []
        for p in filtered:
            ptype = "🧪" if p.is_test_project else "📝"
            framework = getattr(p, 'target_framework', '—')
            data.append({
                "": ptype,
                t("project_name"): p.name,
                t("project_framework"): framework,
                t("project_type"): p.project_type.value if hasattr(p, 'project_type') else '—'
            })
        
        st.dataframe(
            data,
            hide_index=True,
            use_container_width=True
        )
    else:
        st.info(t("no_files"))
