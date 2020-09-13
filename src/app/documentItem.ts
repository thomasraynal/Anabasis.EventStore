export interface DocumentItem {
    id: string;
    isUrl: boolean;
    isMainTitle: boolean;
    content: string;
    isSecondaryTitle: boolean;
    isEmphasis: boolean;
    mainTitleId : string;
    secondaryTitleId : string;
    parentId: string;
    documentId: string;
    position: number;
  }