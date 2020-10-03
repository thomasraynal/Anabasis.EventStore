import { DocumentItem } from './documentItem';

export class  DocumentSearchResult {
    predicate: string;
    document: string;
    documentId: string;
    mainTitle: string;
    mainTitleId: string;
    secondaryTitle: string;
    secondaryTitleId: string;
    peek : string

    constructor() {}
}