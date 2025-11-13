<script>
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import { auth, isAuthenticated, currentUser } from '$lib/stores/auth';
    import { fileManager, sortedFiles, selectedCount } from '$lib/stores/fileManager';
    import { uploads } from '$lib/stores/uploads';
    import { notifications } from '$lib/stores/notifications';
    import { t } from '$lib/stores/i18n';
    import { api } from '$lib/services/api';
    
    import Header from '$lib/components/layout/Header.svelte';
    import Sidebar from '$lib/components/layout/Sidebar.svelte';
    import FileList from '$lib/components/file-manager/FileList.svelte';
    import Toolbar from '$lib/components/file-manager/Toolbar.svelte';
    import Breadcrumbs from '$lib/components/file-manager/Breadcrumbs.svelte';
    import ContextMenu from '$lib/components/file-manager/ContextMenu.svelte';
    import UploadProgress from '$lib/components/file-manager/UploadProgress.svelte';
    import NotificationContainer from '$lib/components/ui/NotificationContainer.svelte';
    import Modal from '$lib/components/ui/Modal.svelte';
    import ConfirmDialog from '$lib/components/ui/ConfirmDialog.svelte';
    import RenameDialog from '$lib/components/ui/RenameDialog.svelte';
    import CreateFolderDialog from '$lib/components/ui/CreateFolderDialog.svelte';
    
    let fileInput;
    let folderInput;
    
    // Context menu state
    let contextMenuVisible = false;
    let contextMenuX = 0;
    let contextMenuY = 0;
    let contextMenuItem = null;
    
    // Dialog states
    let showDeleteDialog = false;
    let showRenameDialog = false;
    let showCreateFolderDialog = false;
    let itemToDelete = null;
    let itemToRename = null;
    
    onMount(async () => {
        if (!$isAuthenticated) {
            goto('/login');
            return;
        }
        
        await loadFiles();
    });
    
    async function loadFiles(folderId = null) {
        fileManager.setLoading(true);
        fileManager.setCurrentFolder(folderId);
        
        try {
            const params = {};
            if (folderId) params.parentId = folderId;
            
            const data = await api.getFiles($currentUser.id, params);
            fileManager.setFiles(data);
        } catch (error) {
            console.error('Error loading files:', error);
            notifications.error($t('operationFailed'));
        } finally {
            fileManager.setLoading(false);
        }
    }
    
    // File operations
    async function handleFolderOpen(folder) {
        const currentPath = $fileManager.breadcrumbPath;
        fileManager.setBreadcrumbPath([...currentPath, folder]);
        await loadFiles(folder.id);
    }
    
    async function handleBreadcrumbNavigate(index) {
        if (index === -1) {
            fileManager.setBreadcrumbPath([]);
            await loadFiles(null);
        } else {
            const newPath = $fileManager.breadcrumbPath.slice(0, index + 1);
            fileManager.setBreadcrumbPath(newPath);
            await loadFiles(newPath[newPath.length - 1].id);
        }
    }
    
    async function handleUpload(files) {
        for (const file of files) {
            const uploadId = Date.now() + Math.random();
            
            uploads.add({
                id: uploadId,
                fileName: file.name,
                fileSize: file.size,
                progress: 0,
                loaded: 0,
                total: file.size,
                status: 'uploading'
            });
            
            try {
                await api.uploadFile(
                    $currentUser.id,
                    file,
                    $fileManager.currentFolderId,
                    (progress, loaded, total) => {
                        uploads.updateProgress(uploadId, progress, loaded, total);
                    }
                );
                
                uploads.setStatus(uploadId, 'completed');
                setTimeout(() => uploads.remove(uploadId), 3000);
            } catch (error) {
                uploads.setStatus(uploadId, 'error');
                notifications.error($t('uploadFailed'));
                setTimeout(() => uploads.remove(uploadId), 5000);
            }
        }
        
        await loadFiles($fileManager.currentFolderId);
    }
    
    function handleUploadFiles() {
        fileInput.click();
    }
    
    function handleUploadFolder() {
        folderInput.click();
    }
    
    async function handleDelete() {
        const selectedItems = Array.from($fileManager.selectedItems);
        itemToDelete = selectedItems;
        showDeleteDialog = true;
    }
    
    async function confirmDelete() {
        try {
            for (const itemId of itemToDelete) {
                await api.deleteItem($currentUser.id, itemId);
            }
            
            notifications.success($t('itemDeleted'));
            fileManager.clearSelection();
            await loadFiles($fileManager.currentFolderId);
        } catch (error) {
            notifications.error($t('operationFailed'));
        }
    }
    
    async function handleRename(item) {
        itemToRename = item;
        showRenameDialog = true;
    }
    
    async function confirmRename({ item, newName }) {
        try {
            await api.renameItem($currentUser.id, item.id, newName);
            notifications.success($t('itemRenamed'));
            await loadFiles($fileManager.currentFolderId);
            showRenameDialog = false;
        } catch (error) {
            notifications.error($t('operationFailed'));
        }
    }
    
    function handleContextMenu({ event, item }) {
        contextMenuVisible = true;
        contextMenuX = event.pageX;
        contextMenuY = event.pageY;
        contextMenuItem = item;
    }
    
    function handleContextMenuAction(action) {
        contextMenuVisible = false;
        
        switch (action) {
            case 'download':
                handleDownload(contextMenuItem);
                break;
            case 'rename':
                handleRename(contextMenuItem);
                break;
            case 'delete':
                itemToDelete = [contextMenuItem.id];
                showDeleteDialog = true;
                break;
        }
    }
</script>

<svelte:head>
    <title>CloudCore Drive</title>
</svelte:head>

<NotificationContainer />

<Header on:search={(e) => console.log('Search:', e.detail)} />

<div class="main">
    <Sidebar 
        currentSection={$fileManager.currentSection}
        on:sectionChange={(e) => fileManager.setSection(e.detail)}
        on:upload={handleUploadFiles}
        on:uploadFolder={handleUploadFolder}
    />
    
    <div class="content">
        <Breadcrumbs 
            path={$fileManager.breadcrumbPath}
            isTrashView={$fileManager.currentSection === 'trash'}
            on:navigate={(e) => handleBreadcrumbNavigate(e.detail)}
        />
        
        <Toolbar 
            selectedCount={$selectedCount}
            isTrashView={$fileManager.currentSection === 'trash'}
            on:refresh={() => loadFiles($fileManager.currentFolderId)}
            on:download={handleDownload}
            on:delete={handleDelete}
            on:rename={() => handleRename(Array.from($fileManager.selectedItems)[0])}
            on:newFolder={() => showCreateFolderDialog = true}
            on:clearSelection={() => fileManager.clearSelection()}
        />
        
        <FileList 
            files={$sortedFiles}
            isLoading={$fileManager.isLoading}
            isTrashView={$fileManager.currentSection === 'trash'}
            bind:selectedItems={$fileManager.selectedItems}
            on:folderOpen={(e) => handleFolderOpen(e.detail)}
            on:contextmenu={handleContextMenu}
        />
    </div>
</div>

<ContextMenu
    visible={contextMenuVisible}
    x={contextMenuX}
    y={contextMenuY}
    item={contextMenuItem}
    isTrashView={$fileManager.currentSection === 'trash'}
    on:action={(e) => handleContextMenuAction(e.detail)}
    on:close={() => contextMenuVisible = false}
/>

<UploadProgress 
    uploads={$uploads}
    on:cancel={(e) => console.log('Cancel upload:', e.detail)}
/>

<ConfirmDialog
    bind:open={showDeleteDialog}
    title={$t('deleteItem')}
    message={$t('deleteConfirmation')}
    confirmText={$t('delete')}
    type="danger"
    on:confirm={confirmDelete}
/>

<RenameDialog
    bind:open={showRenameDialog}
    item={itemToRename}
    on:rename={(e) => confirmRename(e.detail)}
/>

<CreateFolderDialog
    bind:open={showCreateFolderDialog}
    on:create={(e) => console.log('Create folder:', e.detail)}
/>

<!-- Hidden file inputs -->
<input 
    type="file" 
    bind:this={fileInput}
    on:change={(e) => handleUpload(Array.from(e.target.files))}
    multiple 
    style="display: none;"
/>

<input 
    type="file" 
    bind:this={folderInput}
    on:change={(e) => handleUpload(Array.from(e.target.files))}
    webkitdirectory
    style="display: none;"
/>

<style>
    @import '$lib/styles/main.css';
    
    .main {
        display: flex;
        height: calc(100vh - 64px);
    }
    
    .content {
        flex: 1;
        display: flex;
        flex-direction: column;
        overflow: hidden;
    }
</style>
