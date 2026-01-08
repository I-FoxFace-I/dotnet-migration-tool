"""
Sidebar component for Migration Tool.

Handles solution loading, navigation, and language selection.
"""

import streamlit as st
from pathlib import Path

import sys
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from core.solution_parser import SolutionParser
from core.project_parser import ProjectParser
from utils.logging_config import get_logger
from i18n import t, get_available_languages, get_language_display_name

logger = get_logger('ui.sidebar')


def _update_workspace_path():
    """Callback to update workspace path and scan for solutions."""
    if "workspace_path_input" in st.session_state:
        st.session_state.workspace_path = st.session_state.workspace_path_input
        # Clear cached solutions to force rescan
        if "available_solutions" in st.session_state:
            del st.session_state["available_solutions"]


def _scan_for_solutions(workspace_path: str) -> list:
    """Scan workspace for .sln files."""
    path = Path(workspace_path)
    if not path.exists():
        return []
    
    solutions = []
    # Search for .sln files (max 2 levels deep to avoid too many results)
    for sln in path.glob("*.sln"):
        solutions.append(sln)
    for sln in path.glob("*/*.sln"):
        solutions.append(sln)
    
    return sorted(solutions, key=lambda p: p.name)


def _on_language_change():
    """Callback when language is changed."""
    if "language_selector" in st.session_state:
        st.session_state.ui_language = st.session_state.language_selector


def render_sidebar() -> dict:
    """
    Render the sidebar configuration panel.
    
    Returns:
        Configuration dictionary
    """
    with st.sidebar:
        # Language selector at the top
        _render_language_selector()
        
        st.markdown("---")
        st.markdown(f"## {t('sidebar_workspace')}")
        
        # Initialize workspace path in session state if not present
        if "workspace_path" not in st.session_state:
            st.session_state.workspace_path = _get_default_workspace_path()
        
        # Workspace path input
        workspace_path = st.text_input(
            t("workspace_path"),
            value=st.session_state.workspace_path,
            placeholder=t("workspace_path_help"),
            key="workspace_path_input",
            on_change=_update_workspace_path
        )
        
        # Scan for solutions if not cached
        if "available_solutions" not in st.session_state:
            st.session_state.available_solutions = _scan_for_solutions(workspace_path)
        
        # Solution selector
        solutions = st.session_state.available_solutions
        
        if solutions:
            solution_names = [s.name for s in solutions]
            
            # Default selection
            default_idx = 0
            if "selected_solution_name" in st.session_state:
                try:
                    default_idx = solution_names.index(st.session_state.selected_solution_name)
                except ValueError:
                    default_idx = 0
            
            selected_name = st.selectbox(
                t("select_solution"),
                options=solution_names,
                index=default_idx,
                key="solution_selector"
            )
            
            st.session_state.selected_solution_name = selected_name
            selected_solution = solutions[solution_names.index(selected_name)]
        else:
            st.warning(t("no_solutions_found"))
            selected_solution = None
        
        # Load solution button
        col1, col2 = st.columns(2)
        
        with col1:
            load_disabled = selected_solution is None
            if st.button(t("load_solution"), use_container_width=True, disabled=load_disabled):
                if selected_solution:
                    _load_solution(str(selected_solution))
        
        with col2:
            if st.button(t("reload_solution"), use_container_width=True):
                st.session_state.available_solutions = _scan_for_solutions(workspace_path)
                st.rerun()
        
        # Show solution info if loaded
        if st.session_state.solution:
            st.markdown("---")
            _render_solution_info()
            st.markdown("---")
            _render_navigation()
        
        # Settings section
        st.markdown("---")
        _render_settings()
    
    return {
        "workspace_path": workspace_path,
    }


def _render_language_selector():
    """Render language selector."""
    languages = get_available_languages()
    language_options = {code: get_language_display_name(code) for code in languages}
    
    # Get current language index
    current_lang = st.session_state.get("ui_language", "en")
    current_idx = languages.index(current_lang) if current_lang in languages else 0
    
    st.selectbox(
        t("ui_language"),
        options=languages,
        index=current_idx,
        format_func=lambda x: language_options[x],
        key="language_selector",
        on_change=_on_language_change
    )


def _get_default_workspace_path() -> str:
    """Get default workspace path."""
    # Return the workspace root directory
    workspace_root = Path(__file__).parent.parent.parent.parent
    return str(workspace_root)


def _load_solution(path: str):
    """Load a solution from the given path."""
    if not path:
        st.error(t("error_invalid_path", path=""))
        return
    
    sln_path = Path(path)
    
    if not sln_path.exists():
        st.error(t("error_file_not_found", path=str(sln_path)))
        return
    
    if sln_path.suffix.lower() != '.sln':
        st.error(t("error_invalid_path", path=str(sln_path)))
        return
    
    try:
        with st.spinner(t("loading")):
            # Parse solution
            solution_parser = SolutionParser()
            solution = solution_parser.parse(sln_path)
            
            # Parse each project
            project_parser = ProjectParser()
            for project in solution.projects:
                try:
                    project_parser.enrich_project(project)
                except Exception as e:
                    logger.warning(f"Failed to parse project {project.name}: {e}")
            
            # Store in session state
            st.session_state.solution = solution
            st.session_state.projects = solution.projects
            st.session_state.current_view = "dashboard"
            
            st.success(f"✅ {solution.name} ({solution.project_count} {t('metric_projects').split()[-1]})")
            logger.info(f"Loaded solution: {solution.name} with {solution.project_count} projects")
            
    except Exception as e:
        st.error(t("error_loading_solution", error=str(e)))
        logger.error(f"Failed to load solution: {e}")


def _render_solution_info():
    """Render solution information."""
    solution = st.session_state.solution
    
    st.markdown(f"### 📦 {solution.name}")
    
    col1, col2 = st.columns(2)
    
    with col1:
        st.metric(t("metric_projects").split()[-1], solution.project_count)
    
    with col2:
        test_count = len([p for p in solution.projects if p.is_test_project])
        st.metric(t("metric_test_projects").split()[-1], test_count)


def _render_navigation():
    """Render navigation buttons."""
    st.markdown(f"### {t('sidebar_navigation')}")
    
    if st.button(t("nav_dashboard"), use_container_width=True):
        st.session_state.current_view = "dashboard"
        st.rerun()
    
    if st.button(t("nav_explorer"), use_container_width=True):
        st.session_state.current_view = "explorer"
        st.rerun()
    
    if st.button(t("nav_planner"), use_container_width=True):
        st.session_state.current_view = "migration"
        st.rerun()


def _render_settings():
    """Render settings section."""
    with st.expander(t("nav_settings")):
        st.checkbox(t("setting_backup_files"), value=True, key="backup_files")
        st.checkbox(t("setting_confirm_actions"), value=True, key="confirm_actions")
        st.checkbox(t("setting_git_enabled"), value=True, key="git_enabled")
