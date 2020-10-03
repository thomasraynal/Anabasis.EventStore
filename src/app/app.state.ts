export class AppState {
    constructor(public document: string, public title: string, public subtitle: string) {
    }

    isSearch(): boolean{
        return this.document == 'search';
    }

    equals(state: AppState): boolean {

        return state.document == this.document &&
            state.title == this.title;
    }
}