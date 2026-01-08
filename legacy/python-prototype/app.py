"""
Migration Tool - Main Streamlit Application

Interactive tool for reorganizing .NET projects and solutions.
"""

import streamlit as st
from pathlib import Path
import sys

# Add parent directory to path for imports
_parent = Path(__file__).parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from utils.logging_config import setup_logging
from i18n import t

# Setup logging
setup_logging()


def init_session_state():
    """Initialize session state variables."""
    defaults = {
        "solution": None,
        "projects": [],
        "current_view": "dashboard",
        "ui_language": "en",
        "workspace_path": "",
        "available_solutions": [],
        "selected_solution_name": "",
        "selected_project": None,
        "selected_project_obj": None,
        "project_files": [],
        "migration_plan": [],
        "show_execute_dialog": False,
    }
    
    for key, value in defaults.items():
        if key not in st.session_state:
            st.session_state[key] = value


def main():
    """Main application entry point."""
    # Page config
    st.set_page_config(
        page_title=".NET Migration Tool",
        page_icon="🔄",
        layout="wide",
        initial_sidebar_state="expanded"
    )
    
    # Initialize session state
    init_session_state()
    
    # Custom CSS
    st.markdown("""
    <style>
    /* Main container */
    .main .block-container {
        padding-top: 1rem;
        padding-bottom: 1rem;
    }
    
    /* Sidebar */
    .css-1d391kg {
        padding-top: 1rem;
    }
    
    /* Metrics */
    [data-testid="stMetricValue"] {
        font-size: 1.5rem;
    }
    
    /* Expander headers */
    .streamlit-expanderHeader {
        font-size: 0.9rem;
    }
    
    /* Dataframe */
    .dataframe {
        font-size: 0.85rem;
    }
    
    /* Buttons */
    .stButton > button {
        width: 100%;
    }
    
    /* Code blocks */
    code {
        font-size: 0.8rem;
    }
    
    /* Tab styling */
    .stTabs [data-baseweb="tab-list"] {
        gap: 8px;
    }
    
    .stTabs [data-baseweb="tab"] {
        padding: 8px 16px;
    }
    </style>
    """, unsafe_allow_html=True)
    
    # Import UI components (after session state init)
    from ui.sidebar import render_sidebar
    from ui.dashboard import render_dashboard
    from ui.project_explorer import render_project_explorer
    from ui.migration_planner import render_migration_planner
    
    # Render sidebar
    config = render_sidebar()
    
    # Main content area
    current_view = st.session_state.get("current_view", "dashboard")
    
    if current_view == "dashboard":
        render_dashboard()
    elif current_view == "explorer":
        render_project_explorer()
    elif current_view == "migration":
        render_migration_planner()
    else:
        render_dashboard()
    
    # Footer
    st.markdown("---")
    st.markdown(
        f"<div style='text-align: center; color: #888; font-size: 0.8rem;'>"
        f"🔄 .NET Migration Tool | {t('ui_language')}: {st.session_state.get('ui_language', 'en').upper()}"
        f"</div>",
        unsafe_allow_html=True
    )


if __name__ == "__main__":
    main()
