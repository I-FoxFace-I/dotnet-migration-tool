"""
Project Explorer component for Migration Tool.

Displays detailed project structure with hierarchical file tree.
"""

import streamlit as st
from pathlib import Path
from typing import Optional, Dict, List, Any
from collections import defaultdict

import sys
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from i18n import t
from core.file_scanner import FileScanner
from models.solution import Project
from utils.logging_config import get_logger

logger = get_logger('ui.project_explorer')


def render_project_explorer():
    """Render the project explorer view."""
    st.markdown(f"# {t('explorer_title')}")
    
    solution = st.session_state.get("solution")
    
    if not solution:
        st.info(t("explorer_no_solution"))
        return
    
    # Two-panel layout
    col1, col2 = st.columns([1, 2])
    
    with col1:
        _render_project_tree(solution)
    
    with col2:
        _render_project_details()


def _render_project_tree(solution):
    """Render project tree panel."""
    st.markdown(f"### 📂 {t('metric_projects')}")
    
    projects = solution.projects
    
    # Filter controls
    search = st.text_input(
        t("explorer_filter"),
        placeholder=t("search"),
        key="explorer_search"
    )
    
    col1, col2 = st.columns(2)
    with col1:
        show_tests = st.checkbox(t("explorer_show_tests"), value=True, key="exp_show_tests")
    with col2:
        show_source = st.checkbox(t("explorer_show_source"), value=True, key="exp_show_source")
    
    st.markdown("---")
    
    # Filter and display projects
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
    
    # Group by type
    source_projects = [p for p in filtered if not p.is_test_project]
    test_projects = [p for p in filtered if p.is_test_project]
    
    # Source projects
    if source_projects:
        st.markdown(f"**📝 {t('metric_source_projects')}** ({len(source_projects)})")
        for p in source_projects:
            _render_project_button(p)
    
    # Test projects
    if test_projects:
        st.markdown(f"**🧪 {t('metric_test_projects')}** ({len(test_projects)})")
        for p in test_projects:
            _render_project_button(p)


def _render_project_button(project: Project):
    """Render a single project button."""
    icon = "🧪" if project.is_test_project else "📦"
    
    # Check if this project is selected
    is_selected = st.session_state.get("selected_project") == project.name
    
    button_type = "primary" if is_selected else "secondary"
    
    if st.button(
        f"{icon} {project.name}",
        key=f"proj_{project.name}",
        use_container_width=True,
        type=button_type
    ):
        st.session_state.selected_project = project.name
        st.session_state.selected_project_obj = project
        # Scan project files
        _scan_project_files(project)
        st.rerun()


def _scan_project_files(project: Project):
    """Scan files for a project."""
    try:
        scanner = FileScanner()
        
        # Get project directory
        proj_dir = project.directory if hasattr(project, 'directory') else None
        
        if proj_dir and proj_dir.exists():
            files = scanner.scan_directory(proj_dir, recursive=True)
            st.session_state.project_files = files
            st.session_state.project_directory = proj_dir
            logger.info(f"Scanned {len(files)} files in {project.name}")
        else:
            st.session_state.project_files = []
            st.session_state.project_directory = None
            logger.warning(f"Project directory not found: {proj_dir}")
            
    except Exception as e:
        st.session_state.project_files = []
        st.session_state.project_directory = None
        logger.error(f"Failed to scan project files: {e}")


def _render_project_details():
    """Render project details panel."""
    project = st.session_state.get("selected_project_obj")
    
    if not project:
        st.info(t("explorer_no_solution"))
        return
    
    # Project header
    icon = "🧪" if project.is_test_project else "📦"
    st.markdown(f"## {icon} {project.name}")
    
    # Project info
    _render_project_info(project)
    
    st.markdown("---")
    
    # Tabs for different views
    tab1, tab2, tab3, tab4 = st.tabs([
        f"🗂️ {t('files_title')} (Tree)",
        f"📄 {t('files_title')} (List)",
        f"🏗️ {t('classes_title')}",
        f"✅ {t('metric_tests')}"
    ])
    
    with tab1:
        _render_file_tree_tab()
    
    with tab2:
        _render_files_list_tab()
    
    with tab3:
        _render_classes_tab()
    
    with tab4:
        _render_tests_tab()


def _render_project_info(project: Project):
    """Render project information section."""
    col1, col2 = st.columns(2)
    
    with col1:
        st.markdown(f"**{t('project_path')}:**")
        st.code(str(project.path), language=None)
        
        st.markdown(f"**{t('project_framework')}:**")
        framework = getattr(project, 'target_framework', '—')
        st.text(framework)
    
    with col2:
        st.markdown(f"**{t('project_type')}:**")
        ptype = project.project_type.value if hasattr(project, 'project_type') else '—'
        st.text(ptype)
        
        st.markdown(f"**{t('project_namespace')}:**")
        namespace = getattr(project, 'root_namespace', project.name)
        st.text(namespace)
    
    # References
    with st.expander(f"🔗 {t('dependencies_title')}"):
        col1, col2 = st.columns(2)
        
        with col1:
            st.markdown(f"**{t('project_references')}:**")
            refs = getattr(project, 'project_references', [])
            if refs:
                for ref in refs:
                    name = ref.name if hasattr(ref, 'name') else str(ref)
                    st.text(f"• {name}")
            else:
                st.text("—")
        
        with col2:
            st.markdown(f"**{t('package_references')}:**")
            pkgs = getattr(project, 'package_references', [])
            if pkgs:
                for pkg in pkgs[:10]:
                    name = pkg.name if hasattr(pkg, 'name') else str(pkg)
                    version = getattr(pkg, 'version', '')
                    st.text(f"• {name} {version}")
                if len(pkgs) > 10:
                    st.text(f"... and {len(pkgs) - 10} more")
            else:
                st.text("—")


def _build_file_tree(files: List, project_dir: Path) -> Dict:
    """Build hierarchical file tree structure."""
    tree = {"__files__": [], "__dirs__": {}}
    
    for f in files:
        try:
            rel_path = f.path.relative_to(project_dir)
            parts = rel_path.parts
            
            # Navigate/create tree structure
            current = tree
            for i, part in enumerate(parts[:-1]):  # All but last (filename)
                if part not in current["__dirs__"]:
                    current["__dirs__"][part] = {"__files__": [], "__dirs__": {}}
                current = current["__dirs__"][part]
            
            # Add file to current directory
            current["__files__"].append({
                "name": parts[-1],
                "info": f
            })
        except ValueError:
            # File not under project directory
            tree["__files__"].append({
                "name": f.name,
                "info": f
            })
    
    return tree


def _render_file_tree_tab():
    """Render hierarchical file tree."""
    files = st.session_state.get("project_files", [])
    project_dir = st.session_state.get("project_directory")
    
    if not files:
        st.info(t("no_files"))
        return
    
    # Summary metrics
    total_classes = sum(len(f.classes) for f in files)
    total_tests = sum(f.test_count for f in files)
    
    col1, col2, col3 = st.columns(3)
    with col1:
        st.metric(t("files_count").format(count=""), len(files))
    with col2:
        st.metric(t("classes_count").format(count=""), total_classes)
    with col3:
        st.metric(t("tests_count").format(count=""), total_tests)
    
    st.markdown("---")
    
    # Expand/collapse controls
    col1, col2 = st.columns(2)
    with col1:
        if st.button(t("explorer_expand_all"), key="expand_all"):
            st.session_state.tree_expanded = True
    with col2:
        if st.button(t("explorer_collapse_all"), key="collapse_all"):
            st.session_state.tree_expanded = False
    
    default_expanded = st.session_state.get("tree_expanded", True)
    
    st.markdown("---")
    
    # Build and render tree
    if project_dir:
        tree = _build_file_tree(files, project_dir)
        _render_tree_node(tree, "", project_dir, default_expanded)
    else:
        # Fallback to flat list
        for f in files:
            _render_file_item_compact(f)


def _render_tree_node(node: Dict, path: str, project_dir: Path, default_expanded: bool, level: int = 0):
    """Recursively render a tree node."""
    indent = "  " * level
    
    # Sort directories first, then files
    dirs = sorted(node.get("__dirs__", {}).keys())
    files = sorted(node.get("__files__", []), key=lambda x: x["name"])
    
    # Render directories
    for dir_name in dirs:
        dir_node = node["__dirs__"][dir_name]
        dir_path = f"{path}/{dir_name}" if path else dir_name
        
        # Count items in directory
        file_count = _count_files_recursive(dir_node)
        class_count = _count_classes_recursive(dir_node)
        test_count = _count_tests_recursive(dir_node)
        
        # Create unique key for expander
        expander_key = f"dir_{dir_path}".replace("/", "_").replace("\\", "_")
        
        # Directory expander
        with st.expander(
            f"📁 **{dir_name}** ({file_count} files, {class_count} classes, {test_count} tests)",
            expanded=default_expanded
        ):
            _render_tree_node(dir_node, dir_path, project_dir, default_expanded, level + 1)
    
    # Render files
    for file_data in files:
        _render_file_item_tree(file_data["info"], level)


def _count_files_recursive(node: Dict) -> int:
    """Count files recursively in a tree node."""
    count = len(node.get("__files__", []))
    for dir_node in node.get("__dirs__", {}).values():
        count += _count_files_recursive(dir_node)
    return count


def _count_classes_recursive(node: Dict) -> int:
    """Count classes recursively in a tree node."""
    count = sum(len(f["info"].classes) for f in node.get("__files__", []))
    for dir_node in node.get("__dirs__", {}).values():
        count += _count_classes_recursive(dir_node)
    return count


def _count_tests_recursive(node: Dict) -> int:
    """Count tests recursively in a tree node."""
    count = sum(f["info"].test_count for f in node.get("__files__", []))
    for dir_node in node.get("__dirs__", {}).values():
        count += _count_tests_recursive(dir_node)
    return count


def _render_file_item_tree(file_info, level: int = 0):
    """Render a file item in tree view."""
    # File icon based on content
    if file_info.test_count > 0:
        icon = "🧪"
    elif len(file_info.classes) > 0:
        icon = "📄"
    else:
        icon = "📝"
    
    # Summary info
    info_parts = []
    if file_info.classes:
        info_parts.append(f"{len(file_info.classes)} cls")
    if file_info.test_count > 0:
        info_parts.append(f"{file_info.test_count} tests")
    
    info_str = f" ({', '.join(info_parts)})" if info_parts else ""
    
    with st.expander(f"{icon} {file_info.name}{info_str}", expanded=False):
        col1, col2 = st.columns(2)
        
        with col1:
            st.markdown(f"**Namespace:** `{file_info.namespace or '—'}`")
            st.markdown(f"**Lines:** {file_info.line_count}")
        
        with col2:
            st.markdown(f"**Classes:** {len(file_info.classes)}")
            st.markdown(f"**Tests:** {file_info.test_count}")
        
        # Show classes
        if file_info.classes:
            st.markdown("**Members:**")
            for cls in file_info.classes:
                type_label = cls.member_type.value if hasattr(cls, 'member_type') else 'class'
                type_icon = _get_member_icon(type_label)
                
                tests_info = ""
                if hasattr(cls, 'tests') and cls.tests:
                    tests_info = f" ({len(cls.tests)} tests)"
                
                st.markdown(f"  {type_icon} `{cls.name}` *{type_label}*{tests_info}")


def _get_member_icon(member_type: str) -> str:
    """Get icon for member type."""
    icons = {
        "class": "🔷",
        "interface": "🔶",
        "enum": "📊",
        "struct": "📦",
        "record": "📋",
    }
    return icons.get(member_type, "•")


def _render_file_item_compact(file_info):
    """Render a compact file item."""
    icon = "🧪" if file_info.test_count > 0 else "📄"
    st.text(f"{icon} {file_info.name} ({len(file_info.classes)} cls, {file_info.test_count} tests)")


def _render_files_list_tab():
    """Render flat file list tab."""
    files = st.session_state.get("project_files", [])
    
    if not files:
        st.info(t("no_files"))
        return
    
    # Summary
    total_classes = sum(len(f.classes) for f in files)
    total_tests = sum(f.test_count for f in files)
    
    col1, col2, col3 = st.columns(3)
    with col1:
        st.metric(t("files_count").format(count=""), len(files))
    with col2:
        st.metric(t("classes_count").format(count=""), total_classes)
    with col3:
        st.metric(t("tests_count").format(count=""), total_tests)
    
    st.markdown("---")
    
    # Search and filter
    search = st.text_input(
        t("search"),
        placeholder=t("filter"),
        key="file_search"
    )
    
    # Filter files
    filtered = files
    if search:
        filtered = [f for f in files if search.lower() in f.name.lower() or 
                   (f.namespace and search.lower() in f.namespace.lower())]
    
    # Display as table
    if filtered:
        data = []
        for f in filtered[:100]:
            data.append({
                "📄": "🧪" if f.test_count > 0 else "📄",
                "File": f.name,
                "Namespace": f.namespace or "—",
                "Classes": len(f.classes),
                "Tests": f.test_count,
                "Lines": f.line_count
            })
        
        st.dataframe(
            data,
            hide_index=True,
            use_container_width=True
        )
        
        if len(filtered) > 100:
            st.info(f"... and {len(filtered) - 100} more files")
    else:
        st.info(t("no_files"))


def _render_classes_tab():
    """Render classes tab content."""
    files = st.session_state.get("project_files", [])
    
    if not files:
        st.info(t("no_files"))
        return
    
    # Collect all classes
    all_classes = []
    for f in files:
        for cls in f.classes:
            all_classes.append({
                'name': cls.name,
                'namespace': cls.namespace if hasattr(cls, 'namespace') else f.namespace,
                'type': cls.member_type.value if hasattr(cls, 'member_type') else 'class',
                'file': f.name,
                'tests': len(cls.tests) if hasattr(cls, 'tests') else 0
            })
    
    if not all_classes:
        st.info(t("no_files"))
        return
    
    # Summary
    st.metric(t("classes_count").format(count=""), len(all_classes))
    
    st.markdown("---")
    
    # Filter
    col1, col2 = st.columns([2, 1])
    
    with col1:
        search = st.text_input(
            t("search"),
            placeholder=t("filter"),
            key="class_search"
        )
    
    with col2:
        type_filter = st.multiselect(
            t("project_type"),
            options=['class', 'interface', 'enum', 'struct', 'record'],
            default=['class', 'interface', 'enum', 'struct', 'record'],
            key="class_type_filter"
        )
    
    # Filter classes
    filtered = all_classes
    if search:
        filtered = [c for c in filtered if search.lower() in c['name'].lower() or
                   (c['namespace'] and search.lower() in c['namespace'].lower())]
    if type_filter:
        filtered = [c for c in filtered if c['type'] in type_filter]
    
    # Display as table
    if filtered:
        # Add icon column
        data = []
        for c in filtered[:100]:
            data.append({
                "": _get_member_icon(c['type']),
                t("project_name"): c['name'],
                t("project_namespace"): c['namespace'] or "—",
                t("project_type"): c['type'],
                "File": c['file'],
                t("metric_tests"): c['tests']
            })
        
        st.dataframe(
            data,
            hide_index=True,
            use_container_width=True
        )
        
        if len(filtered) > 100:
            st.info(f"... and {len(filtered) - 100} more classes")
    else:
        st.info(t("no_files"))


def _render_tests_tab():
    """Render tests tab content."""
    files = st.session_state.get("project_files", [])
    
    if not files:
        st.info(t("no_files"))
        return
    
    # Collect all tests
    all_tests = []
    for f in files:
        for cls in f.classes:
            if hasattr(cls, 'tests'):
                for test in cls.tests:
                    all_tests.append({
                        'name': test.name if hasattr(test, 'name') else str(test),
                        'class': cls.name,
                        'file': f.name,
                        'framework': test.framework.value if hasattr(test, 'framework') else 'xunit'
                    })
    
    if not all_tests:
        st.info(t("no_files"))
        return
    
    # Summary
    st.metric(t("tests_count").format(count=""), len(all_tests))
    
    st.markdown("---")
    
    # Filter
    search = st.text_input(
        t("search"),
        placeholder=t("filter"),
        key="test_search"
    )
    
    # Filter tests
    filtered = all_tests
    if search:
        filtered = [t for t in filtered if search.lower() in t['name'].lower() or 
                   search.lower() in t['class'].lower()]
    
    # Group by class
    by_class = defaultdict(list)
    for test in filtered:
        by_class[test['class']].append(test)
    
    # Display grouped
    for class_name, tests in sorted(by_class.items()):
        with st.expander(f"🧪 **{class_name}** ({len(tests)} tests)"):
            for test in tests:
                st.markdown(f"  ✅ `{test['name']}`")
    
    # Also show as table
    st.markdown("---")
    st.markdown("**All Tests:**")
    
    if filtered:
        data = []
        for t in filtered[:100]:
            data.append({
                "Test": t['name'],
                "Class": t['class'],
                "File": t['file'],
                "Framework": t['framework']
            })
        
        st.dataframe(
            data,
            hide_index=True,
            use_container_width=True
        )
        
        if len(filtered) > 100:
            st.info(f"... and {len(filtered) - 100} more tests")
