"""
Migration Planner component for Migration Tool.

Allows planning and executing project migrations.
"""

import streamlit as st
from pathlib import Path
from typing import List, Dict, Optional
from dataclasses import dataclass, field
from enum import Enum
import json
import os
import re

import sys
_parent = Path(__file__).parent.parent
if str(_parent) not in sys.path:
    sys.path.insert(0, str(_parent))

from i18n import t
from utils.logging_config import get_logger

logger = get_logger('ui.migration_planner')


class MigrationAction(Enum):
    """Migration action types."""
    MOVE_FILE = "move_file"
    MOVE_FOLDER = "move_folder"
    CREATE_PROJECT = "create_project"
    DELETE_PROJECT = "delete_project"
    RENAME_NAMESPACE = "rename_namespace"
    ADD_REFERENCE = "add_reference"
    REMOVE_REFERENCE = "remove_reference"


class StepStatus(Enum):
    """Migration step status."""
    PENDING = "pending"
    IN_PROGRESS = "in_progress"
    COMPLETED = "completed"
    FAILED = "failed"
    SKIPPED = "skipped"


@dataclass
class MigrationStep:
    """A single migration step."""
    id: int
    action: MigrationAction
    source: str
    target: str
    status: StepStatus = StepStatus.PENDING
    error: Optional[str] = None
    metadata: Dict = field(default_factory=dict)


def render_migration_planner():
    """Render the migration planner view."""
    st.markdown(f"# {t('planner_title')}")
    
    solution = st.session_state.get("solution")
    
    if not solution:
        st.info(t("planner_no_solution"))
        return
    
    # Initialize migration plan in session state
    if "migration_plan" not in st.session_state:
        st.session_state.migration_plan = []
    
    # Three-panel layout
    col1, col2, col3 = st.columns([1, 1, 1])
    
    with col1:
        _render_source_panel(solution)
    
    with col2:
        _render_actions_panel()
    
    with col3:
        _render_target_panel(solution)
    
    st.markdown("---")
    
    # Migration plan
    _render_migration_plan()


def _render_source_panel(solution):
    """Render source selection panel."""
    st.markdown(f"### {t('planner_source')}")
    
    # Project selector
    projects = solution.projects
    project_names = [""] + [p.name for p in projects]
    
    selected_project = st.selectbox(
        t("select_solution"),
        options=project_names,
        key="source_project"
    )
    
    if selected_project:
        # Find project
        project = next((p for p in projects if p.name == selected_project), None)
        
        if project:
            # Show project files
            st.markdown(f"**📄 {t('files_title')}**")
            
            # Get files from session state if already scanned
            files = st.session_state.get(f"files_{selected_project}", [])
            
            if not files:
                # Scan files
                from core.file_scanner import FileScanner
                scanner = FileScanner()
                
                proj_dir = project.directory if hasattr(project, 'directory') else None
                if proj_dir and proj_dir.exists():
                    files = scanner.scan_directory(proj_dir, recursive=True)
                    st.session_state[f"files_{selected_project}"] = files
            
            # File tree with checkboxes
            if files:
                # Group by folder
                folders = {}
                for f in files:
                    rel_path = f.path.relative_to(project.directory) if hasattr(project, 'directory') else f.path
                    folder = str(rel_path.parent) if rel_path.parent != Path('.') else "Root"
                    
                    if folder not in folders:
                        folders[folder] = []
                    folders[folder].append(f)
                
                # Display folders with checkboxes for folder selection
                for folder, folder_files in sorted(folders.items()):
                    # Folder checkbox for Move Folder action
                    folder_path = os.path.join(str(project.directory), folder) if hasattr(project, 'directory') and folder != "Root" else None
                    
                    col1, col2 = st.columns([0.1, 0.9])
                    with col1:
                        if folder != "Root" and folder_path:
                            st.checkbox(
                                "",
                                key=f"source_folder_{folder_path}",
                                value=False,
                                label_visibility="collapsed"
                            )
                    with col2:
                        with st.expander(f"📁 {folder} ({len(folder_files)})", expanded=False):
                            for f in folder_files:
                                st.checkbox(
                                    f"📄 {f.name}",
                                    key=f"source_file_{f.path}",
                                    value=False
                                )
            else:
                st.info(t("no_files"))


def _render_actions_panel():
    """Render actions panel."""
    st.markdown(f"### {t('planner_actions')}")
    
    # Action buttons
    actions = [
        (MigrationAction.MOVE_FILE, t("action_move_file"), "📄"),
        (MigrationAction.MOVE_FOLDER, t("action_move_folder"), "📁"),
        (MigrationAction.CREATE_PROJECT, t("action_create_project"), "➕"),
        (MigrationAction.DELETE_PROJECT, t("action_delete_project"), "🗑️"),
        (MigrationAction.RENAME_NAMESPACE, t("action_rename_namespace"), "✏️"),
        (MigrationAction.ADD_REFERENCE, t("action_add_reference"), "🔗"),
        (MigrationAction.REMOVE_REFERENCE, t("action_remove_reference"), "❌"),
    ]
    
    for action, label, icon in actions:
        if st.button(
            f"{icon} {label}",
            key=f"action_{action.value}",
            use_container_width=True
        ):
            st.session_state.selected_action = action
            st.rerun()
    
    # Show selected action details
    selected_action = st.session_state.get("selected_action")
    
    if selected_action:
        st.markdown("---")
        st.markdown(f"**Selected:** {selected_action.value}")
        
        # Action-specific inputs
        if selected_action == MigrationAction.CREATE_PROJECT:
            _render_create_project_form()
        elif selected_action == MigrationAction.RENAME_NAMESPACE:
            _render_rename_namespace_form()
        elif selected_action in [MigrationAction.MOVE_FILE, MigrationAction.MOVE_FOLDER]:
            _render_move_form(selected_action)


def _render_create_project_form():
    """Render create project form."""
    st.markdown("#### Create New Project")
    
    project_name = st.text_input("Project Name", key="new_project_name")
    project_type = st.selectbox(
        t("project_type"),
        options=["classlib", "console", "wpf", "test"],
        key="new_project_type"
    )
    
    if st.button(t("plan_add_step"), key="add_create_step"):
        if project_name:
            _add_migration_step(
                MigrationAction.CREATE_PROJECT,
                source="",
                target=project_name,
                metadata={"type": project_type}
            )
            st.success(f"Added: Create project '{project_name}'")


def _render_rename_namespace_form():
    """Render rename namespace form."""
    st.markdown("#### Rename Namespace")
    
    old_ns = st.text_input("Old Namespace", key="old_namespace")
    new_ns = st.text_input("New Namespace", key="new_namespace")
    
    if st.button(t("plan_add_step"), key="add_rename_step"):
        if old_ns and new_ns:
            _add_migration_step(
                MigrationAction.RENAME_NAMESPACE,
                source=old_ns,
                target=new_ns
            )
            st.success(f"Added: Rename '{old_ns}' -> '{new_ns}'")


def _render_move_form(action: MigrationAction):
    """Render move file/folder form."""
    item_type = "File" if action == MigrationAction.MOVE_FILE else "Folder"
    st.markdown(f"#### Move {item_type}")
    
    # Get selected items from source panel based on action type
    selected_items = []
    
    if action == MigrationAction.MOVE_FOLDER:
        # Get selected folders
        for key, value in st.session_state.items():
            if key.startswith("source_folder_") and value:
                selected_items.append(key.replace("source_folder_", ""))
    else:
        # Get selected files
        for key, value in st.session_state.items():
            if key.startswith("source_file_") and value:
                selected_items.append(key.replace("source_file_", ""))
    
    if selected_items:
        st.markdown(f"**Selected:** {len(selected_items)} {item_type.lower()}(s)")
        for item in selected_items:
            st.text(f"  • {Path(item).name}")
        
        solution = st.session_state.get("solution")
        projects = solution.projects if solution else []
        
        target_project = st.selectbox(
            "Target Project",
            options=projects,
            format_func=lambda p: p.name,
            key="move_target_project"
        )
        
        # Show selected target project directory for clarity
        if target_project:
            st.caption(f"📁 Target: {target_project.directory}")
        
        target_folder = st.text_input("Target Folder (relative path, e.g. 'Helpers' or leave empty)", key="move_target_folder")
        
        if st.button(t("plan_add_step"), key="add_move_step"):
            if not target_project:
                st.error("Please select a target project")
            else:
                for source in selected_items:
                    source_folder_name = os.path.basename(source)
                    # Use target folder name or source folder name as default
                    final_folder = target_folder if target_folder else source_folder_name
                    target_path = os.path.join(str(target_project.directory), final_folder)
                    _add_migration_step(
                        action,
                        source=source,
                        target=target_path
                    )
                st.success(f"Added: Move {len(selected_items)} {item_type.lower()}(s) to {target_project.name}")
    else:
        if action == MigrationAction.MOVE_FOLDER:
            st.info("Select folders using checkboxes next to folder names in the source panel")
        else:
            st.info("Select files using checkboxes in the source panel")


def _render_target_panel(solution):
    """Render target selection panel."""
    st.markdown(f"### {t('planner_target')}")
    
    # Project selector
    projects = solution.projects
    project_names = ["(New Project)"] + [p.name for p in projects]
    
    selected_project = st.selectbox(
        t("select_solution"),
        options=project_names,
        key="target_project"
    )
    
    if selected_project == "(New Project)":
        st.info("Use 'Create Project' action to add a new project")
    elif selected_project:
        # Find project
        project = next((p for p in projects if p.name == selected_project), None)
        
        if project:
            # Show project structure
            st.markdown(f"**📦 {project.name}**")
            
            # Show existing folders
            proj_dir = project.directory if hasattr(project, 'directory') else None
            if proj_dir and proj_dir.exists():
                folders = set()
                for f in proj_dir.rglob("*.cs"):
                    rel = f.relative_to(proj_dir)
                    if rel.parent != Path('.'):
                        folders.add(str(rel.parent))
                
                if folders:
                    st.markdown(f"**📁 Folders:**")
                    for folder in sorted(folders)[:20]:
                        st.text(f"  • {folder}")


def _render_migration_plan():
    """Render the migration plan section."""
    st.markdown(f"## {t('plan_title')}")
    
    plan = st.session_state.get("migration_plan", [])
    
    # Action buttons
    col1, col2, col3, col4 = st.columns(4)
    
    with col1:
        if st.button(t("plan_clear"), use_container_width=True):
            st.session_state.migration_plan = []
            st.rerun()
    
    with col2:
        if st.button(t("plan_export"), use_container_width=True):
            _export_plan()
    
    with col3:
        if st.button(t("plan_import"), use_container_width=True):
            _import_plan()
    
    with col4:
        execute_disabled = len(plan) == 0
        if st.button(t("plan_execute"), use_container_width=True, type="primary", disabled=execute_disabled):
            st.session_state.show_execute_dialog = True
    
    # Show execution dialog
    if st.session_state.get("show_execute_dialog"):
        _render_execute_dialog()
    
    st.markdown("---")
    
    # Plan steps
    if not plan:
        st.info(t("plan_empty"))
        return
    
    # Display steps
    for i, step in enumerate(plan):
        _render_plan_step(i, step)


def _render_plan_step(index: int, step: MigrationStep):
    """Render a single plan step."""
    # Status icon
    status_icons = {
        StepStatus.PENDING: "⏳",
        StepStatus.IN_PROGRESS: "🔄",
        StepStatus.COMPLETED: "✅",
        StepStatus.FAILED: "❌",
        StepStatus.SKIPPED: "⏭️",
    }
    
    status_icon = status_icons.get(step.status, "⏳")
    
    # Action icon
    action_icons = {
        MigrationAction.MOVE_FILE: "📄",
        MigrationAction.MOVE_FOLDER: "📁",
        MigrationAction.CREATE_PROJECT: "➕",
        MigrationAction.DELETE_PROJECT: "🗑️",
        MigrationAction.RENAME_NAMESPACE: "✏️",
        MigrationAction.ADD_REFERENCE: "🔗",
        MigrationAction.REMOVE_REFERENCE: "❌",
    }
    
    action_icon = action_icons.get(step.action, "📝")
    
    with st.container():
        col1, col2, col3, col4, col5 = st.columns([1, 2, 3, 3, 1])
        
        with col1:
            st.markdown(f"**{status_icon} #{step.id}**")
        
        with col2:
            st.markdown(f"{action_icon} {step.action.value}")
        
        with col3:
            st.markdown(f"**{t('step_source')}:** `{step.source or '—'}`")
        
        with col4:
            st.markdown(f"**{t('step_target')}:** `{step.target or '—'}`")
        
        with col5:
            if st.button("🗑️", key=f"remove_step_{index}"):
                st.session_state.migration_plan.pop(index)
                st.rerun()
        
        # Show error if failed
        if step.status == StepStatus.FAILED and step.error:
            st.error(step.error)


def _render_execute_dialog():
    """Render execution confirmation dialog."""
    st.markdown("---")
    st.markdown(f"### {t('execute_title')}")
    
    st.warning(t("execute_warning"))
    st.markdown(t("execute_confirm"))
    
    col1, col2 = st.columns(2)
    
    with col1:
        if st.button(t("execute_start"), type="primary", use_container_width=True):
            st.session_state.show_execute_dialog = False
            _execute_migration_plan()
    
    with col2:
        if st.button(t("execute_cancel"), use_container_width=True):
            st.session_state.show_execute_dialog = False
            st.rerun()


def _add_migration_step(
    action: MigrationAction,
    source: str,
    target: str,
    metadata: Dict = None
):
    """Add a step to the migration plan."""
    plan = st.session_state.get("migration_plan", [])
    
    step = MigrationStep(
        id=len(plan) + 1,
        action=action,
        source=source,
        target=target,
        metadata=metadata or {}
    )
    
    plan.append(step)
    st.session_state.migration_plan = plan
    logger.info(f"Added migration step: {action.value} {source} -> {target}")


def _export_plan():
    """Export migration plan to JSON."""
    plan = st.session_state.get("migration_plan", [])
    
    if not plan:
        st.warning(t("plan_empty"))
        return
    
    # Convert to serializable format
    export_data = []
    for step in plan:
        export_data.append({
            "id": step.id,
            "action": step.action.value,
            "source": step.source,
            "target": step.target,
            "metadata": step.metadata
        })
    
    json_str = json.dumps(export_data, indent=2)
    
    st.download_button(
        label="📥 Download JSON",
        data=json_str,
        file_name="migration_plan.json",
        mime="application/json"
    )


def _import_plan():
    """Import migration plan from JSON."""
    uploaded = st.file_uploader(
        "Upload migration plan",
        type=["json"],
        key="import_plan_file"
    )
    
    if uploaded:
        try:
            data = json.load(uploaded)
            
            plan = []
            for item in data:
                step = MigrationStep(
                    id=item["id"],
                    action=MigrationAction(item["action"]),
                    source=item["source"],
                    target=item["target"],
                    metadata=item.get("metadata", {})
                )
                plan.append(step)
            
            st.session_state.migration_plan = plan
            st.success(f"Imported {len(plan)} steps")
            st.rerun()
            
        except Exception as e:
            st.error(f"Failed to import: {e}")


def _execute_migration_plan():
    """Execute the migration plan."""
    plan = st.session_state.get("migration_plan", [])
    
    if not plan:
        return
    
    progress_bar = st.progress(0)
    status_text = st.empty()
    
    total = len(plan)
    completed = 0
    
    for i, step in enumerate(plan):
        status_text.text(t("execute_progress", current=i+1, total=total))
        
        try:
            # Update status
            step.status = StepStatus.IN_PROGRESS
            
            # Execute step (placeholder - actual implementation would go here)
            _execute_step(step)
            
            step.status = StepStatus.COMPLETED
            completed += 1
            
        except Exception as e:
            step.status = StepStatus.FAILED
            step.error = str(e)
            logger.error(f"Migration step failed: {e}")
            
            # Show error and stop
            st.error(t("execute_failure", step=step.id, error=str(e)))
            break
        
        progress_bar.progress((i + 1) / total)
    
    # Final status
    if completed == total:
        st.success(t("execute_success"))
    
    st.session_state.migration_plan = plan


def _execute_step(step: MigrationStep):
    """Execute a single migration step."""
    logger.info(f"Executing step {step.id}: {step.action.value}")
    logger.debug(f"  Source: {step.source}")
    logger.debug(f"  Target: {step.target}")
    
    # Get workspace root from session state (try multiple keys)
    workspace_root = st.session_state.get("workspace_path") or st.session_state.get("workspace_path_input")
    
    # If still not set, try to infer from source path
    if not workspace_root and step.source:
        # Source path is absolute, find workspace root from it
        source_path = Path(step.source)
        if source_path.is_absolute():
            # Use parent directories to find a reasonable workspace root
            workspace_root = str(source_path.parent.parent.parent)
            logger.info(f"Inferred workspace root from source: {workspace_root}")
    
    if not workspace_root:
        logger.error("Workspace path not set in session state")
        raise Exception("Workspace path not set. Please set workspace path in sidebar.")
    
    # Import migration engine
    from core.migration_engine import MigrationEngine
    
    engine = MigrationEngine(Path(workspace_root), dry_run=False)
    
    if step.action == MigrationAction.MOVE_FILE:
        result = engine.move_file(Path(step.source), Path(step.target))
        if not result.success:
            raise Exception(result.message)
            
    elif step.action == MigrationAction.MOVE_FOLDER:
        # Source and target are absolute paths, engine expects them as-is for absolute paths
        source_path = Path(step.source)
        target_path = Path(step.target)
        
        logger.info(f"Moving folder from {source_path} to {target_path}")
        
        # Use shutil directly for absolute paths
        if source_path.is_absolute():
            if not source_path.exists():
                raise Exception(f"Source folder does not exist: {source_path}")
            if target_path.exists():
                raise Exception(f"Target folder already exists: {target_path}")
            
            # Create parent directories
            target_path.parent.mkdir(parents=True, exist_ok=True)
            
            # Move the folder
            import shutil
            shutil.move(str(source_path), str(target_path))
            logger.info(f"Successfully moved folder: {source_path} -> {target_path}")
        else:
            # Relative paths - use engine
            result = engine.move_folder(source_path, target_path)
            if not result.success:
                raise Exception(result.message)
        
        # After moving, update references in affected projects
        _update_references_after_move(engine, step.source, step.target)
            
    elif step.action == MigrationAction.CREATE_PROJECT:
        # TODO: Implement project creation using dotnet new
        logger.warning("CREATE_PROJECT not yet implemented")
        
    elif step.action == MigrationAction.DELETE_PROJECT:
        # TODO: Implement project deletion
        logger.warning("DELETE_PROJECT not yet implemented")
        
    elif step.action == MigrationAction.RENAME_NAMESPACE:
        result = engine.rename_namespace(
            Path(step.source),
            step.metadata.get("old_namespace", ""),
            step.metadata.get("new_namespace", step.target)
        )
        if not result.success:
            raise Exception(result.message)
            
    elif step.action == MigrationAction.ADD_REFERENCE:
        # TODO: Implement reference addition
        logger.warning("ADD_REFERENCE not yet implemented")
        
    elif step.action == MigrationAction.REMOVE_REFERENCE:
        # TODO: Implement reference removal
        logger.warning("REMOVE_REFERENCE not yet implemented")
    
    logger.info(f"Step {step.id} completed")


def _update_references_after_move(engine, old_path: str, new_path: str):
    """Update all project references after a folder move."""
    from pathlib import Path
    
    # Find the .csproj file in the moved folder
    old_project_name = Path(old_path).name
    new_project_dir = Path(new_path)
    
    # Calculate the relative path change
    old_parts = Path(old_path).parts
    new_parts = Path(new_path).parts
    
    # Find all .csproj files in the workspace
    workspace = engine.workspace_root
    
    for csproj in workspace.rglob("*.csproj"):
        try:
            content = csproj.read_text(encoding='utf-8')
            
            # Check if this project references the moved project
            if old_project_name in content:
                # Calculate relative paths
                csproj_dir = csproj.parent
                
                # Try to find and update the reference
                old_ref_pattern = rf'Include="([^"]*{re.escape(old_project_name)}[^"]*\.csproj)"'
                
                def replace_ref(match):
                    old_ref = match.group(1)
                    # Calculate new relative path
                    try:
                        # Resolve the old reference to absolute path
                        old_abs = (csproj_dir / old_ref).resolve()
                        
                        # Check if it was pointing to the old location
                        old_expected = (workspace / old_path / f"{old_project_name}.csproj").resolve()
                        
                        if old_abs == old_expected:
                            # Calculate new relative path
                            new_abs = workspace / new_path / f"{old_project_name}.csproj"
                            new_rel = os.path.relpath(new_abs, csproj_dir)
                            return f'Include="{new_rel}"'
                    except Exception:
                        pass
                    return match.group(0)
                
                updated = re.sub(old_ref_pattern, replace_ref, content)
                
                if content != updated:
                    csproj.write_text(updated, encoding='utf-8')
                    logger.info(f"Updated references in {csproj.relative_to(workspace)}")
                    
        except Exception as e:
            logger.warning(f"Failed to update {csproj}: {e}")
    
    # Update solution files
    for sln in engine.find_solution_files():
        engine.update_solution_project_path(
            sln,
            old_path.replace('/', '\\'),
            new_path.replace('/', '\\'),
            old_project_name
        )
