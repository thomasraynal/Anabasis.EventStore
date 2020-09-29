import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { List } from 'linqts';
import { AppState } from './app.state';
import { Subject } from 'rxjs/internal/Subject';


@Injectable({
    providedIn: 'root'
})
export class AppStateService {

    state: Observable<AppState>;

    private _internalState = new Subject<AppState>()

    constructor() {
        this._internalState = new Subject();
        this.state = this._internalState;
    }

    change(state:AppState) {

        this._internalState.next(state);

    }

}