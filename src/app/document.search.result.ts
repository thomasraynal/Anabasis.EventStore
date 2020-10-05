import { DocumentItem } from './documentItem';

export class DocumentSearchResult {
    predicate: string;
    document: string;
    documentId: string;
    mainTitle: string;
    mainTitleId: string;
    secondaryTitle: string;
    secondaryTitleId: string;
    peek: string

    link() {

        if (!this.secondaryTitleId) return '/' + this.documentId + '/' + this.mainTitleId;

        return '/' + this.documentId + '/' + this.mainTitleId + '/' + this.secondaryTitleId;
    }

    constructor() { }
}