import { DocumentItem } from './documentItem';

export class  DocumentSearchResult {
    predicate: string;
    documentName: string;
    mainTitle: string;
    secondaryTitle: string;
    isMainTitle: boolean;
    isSecondaryTitle: boolean;
    menuId: string;
    peek : string

    constructor() {}
}